import crypto from "node:crypto";
import { DynamoDBClient, GetItemCommand, PutItemCommand } from "@aws-sdk/client-dynamodb";

const ddb = new DynamoDBClient({});

const tokenTable = (process.env.ALEXA_TOKEN_TABLE || "NeoTwitchAlexaTokens").trim();
const relayToken = (process.env.NEO_TWITCH_RELAY_TOKEN || "").trim();
const alexaClientId = (process.env.ALEXA_EVENT_CLIENT_ID || "").trim();
const alexaClientSecret = (process.env.ALEXA_EVENT_CLIENT_SECRET || "").trim();
const alexaRegion = (process.env.ALEXA_EVENT_REGION || "NA").trim().toUpperCase();

const endpointId = "neo-twitch-events";
const tokenRecordId = "default";
const lwaTokenUrl = "https://api.amazon.com/auth/o2/token";

const eventGateways = {
  NA: "https://api.amazonalexa.com/v3/events",
  EU: "https://api.eu.amazonalexa.com/v3/events",
  FE: "https://api.fe.amazonalexa.com/v3/events"
};

const twitchEvents = [
  { id: "seguidor", instance: "neo-twitch.seguidor", name: "Seguidor" },
  { id: "suscripcion", instance: "neo-twitch.suscripcion", name: "Suscripcion" },
  { id: "raid", instance: "neo-twitch.raid", name: "Raid" },
  { id: "bits100", instance: "neo-twitch.bits100", name: "Bits 100" },
  { id: "canje_susto", instance: "neo-twitch.canje_susto", name: "Canje susto" },
  { id: "luz_encendida", instance: "neo-twitch.luz_encendida", name: "Luz encendida" },
  { id: "luz_apagada", instance: "neo-twitch.luz_apagada", name: "Luz apagada" }
];

export const handler = async (event) => {
  try {
    if (event?.directive) {
      const header = event.directive.header || {};
      console.info("Alexa directive received", {
        namespace: header.namespace,
        name: header.name,
        messageId: header.messageId
      });
      return await handleAlexaDirective(event);
    }

    if (event?.requestContext?.http || event?.body !== undefined) {
      console.info("Neo Twitch HTTP event received");
      return await handleNeoTwitchHttp(event);
    }

    console.warn("Unknown Lambda input", Object.keys(event || {}));
    return httpResponse(400, { ok: false, error: "unknown_input" });
  } catch (error) {
    console.error("Neo Twitch Alexa relay failed", error);

    if (event?.directive) {
      return alexaErrorResponse("INTERNAL_ERROR", error instanceof Error ? error.message : "Unknown error");
    }

    return httpResponse(500, {
      ok: false,
      error: error instanceof Error ? error.message : "Unknown error"
    });
  }
};

async function handleAlexaDirective(request) {
  const directive = request.directive;
  const namespace = directive?.header?.namespace;
  const name = directive?.header?.name;

  if (namespace === "Alexa.Discovery" && name === "Discover") {
    return discoveryResponse();
  }

  if (namespace === "Alexa.Authorization" && name === "AcceptGrant") {
    await acceptGrant(directive);
    return alexaResponse("Alexa.Authorization", "AcceptGrant.Response");
  }

  return alexaErrorResponse("INVALID_DIRECTIVE", `Unsupported directive ${namespace}.${name}`);
}

async function handleNeoTwitchHttp(event) {
  if (relayToken) {
    const bearer = getBearerToken(event.headers || {});
    if (bearer !== relayToken) {
      return httpResponse(401, { ok: false, error: "invalid_token" });
    }
  }

  const body = parseBody(event);
  const eventName = String(body.eventName || "").trim();
  const normalizedEventName = normalizeEventId(eventName);
  const definition = twitchEvents.find((item) =>
    normalizeEventId(item.id) === normalizedEventName
    || normalizeEventId(item.name) === normalizedEventName);

  if (!definition) {
    return httpResponse(400, {
      ok: false,
      error: "unknown_event",
      eventName,
      supportedEvents: twitchEvents.map((item) => item.id)
    });
  }

  const tokens = await getValidAlexaTokens();
  const gatewayUrl = eventGateways[alexaRegion] || eventGateways.NA;
  const alexaEvent = buildSimpleEvent(definition, tokens.accessToken);
  const result = await postAlexaEvent(gatewayUrl, alexaEvent, tokens.accessToken);

  return httpResponse(202, {
    ok: true,
    eventName,
    alexaStatus: result.status
  });
}

function discoveryResponse() {
  return {
    event: {
      header: {
        namespace: "Alexa.Discovery",
        name: "Discover.Response",
        payloadVersion: "3",
        messageId: crypto.randomUUID()
      },
      payload: {
        endpoints: [
          {
            endpointId,
            manufacturerName: "Neo Twitch",
            description: "Eventos de Twitch para rutinas de Alexa",
            friendlyName: "Neo Twitch",
            displayCategories: ["REMOTE"],
            additionalAttributes: {
              manufacturer: "Neo Twitch",
              model: "Neo Twitch Alexa Relay",
              serialNumber: "neo-twitch-events",
              firmwareVersion: "1.0",
              softwareVersion: "1.0"
            },
            cookie: {},
            capabilities: [
              ...twitchEvents.map((eventDefinition) => simpleEventCapability(eventDefinition)),
              {
                type: "AlexaInterface",
                interface: "Alexa.EndpointHealth",
                version: "3.2",
                properties: {
                  supported: [{ name: "connectivity" }],
                  proactivelyReported: true,
                  retrievable: true
                }
              },
              {
                type: "AlexaInterface",
                interface: "Alexa",
                version: "3"
              }
            ]
          }
        ]
      }
    }
  };
}

function simpleEventCapability(eventDefinition) {
  return {
    type: "AlexaInterface",
    interface: "Alexa.SimpleEventSource",
    instance: eventDefinition.instance,
    version: "1.0",
    properties: {},
    capabilityResources: {
      friendlyNames: [textLabel(eventDefinition.name)]
    },
    configuration: {
      supportedEvents: [
        {
          id: eventDefinition.id,
          friendlyNames: [textLabel(eventDefinition.name)]
        }
      ]
    }
  };
}

function buildSimpleEvent(eventDefinition, accessToken) {
  return {
    event: {
      header: {
        namespace: "Alexa.SimpleEventSource",
        name: "Event",
        instance: eventDefinition.instance,
        messageId: crypto.randomUUID(),
        payloadVersion: "1.0"
      },
      endpoint: {
        scope: {
          type: "BearerToken",
          token: accessToken
        },
        endpointId,
        cookie: {}
      },
      payload: {
        id: eventDefinition.id,
        timestamp: new Date().toISOString()
      }
    }
  };
}

async function acceptGrant(directive) {
  const code = directive?.payload?.grant?.code;
  if (!code) {
    throw new Error("AcceptGrant did not include grant.code");
  }

  const tokenResponse = await requestLwaTokens({
    grant_type: "authorization_code",
    code,
    client_id: alexaClientId,
    client_secret: alexaClientSecret
  });

  await saveTokens(tokenResponse);
}

async function getValidAlexaTokens() {
  const record = await getTokenRecord();

  if (!record?.refreshToken) {
    throw new Error("No Alexa tokens found. Enable and link the skill from the Alexa app first.");
  }

  const refreshAt = Number(record.expiresAt || 0) - 5 * 60 * 1000;
  if (record.accessToken && Date.now() < refreshAt) {
    return record;
  }

  const refreshed = await requestLwaTokens({
    grant_type: "refresh_token",
    refresh_token: record.refreshToken,
    client_id: alexaClientId,
    client_secret: alexaClientSecret
  });

  await saveTokens({
    ...refreshed,
    refresh_token: refreshed.refresh_token || record.refreshToken
  });
  return getTokenRecord();
}

async function requestLwaTokens(values) {
  if (!alexaClientId || !alexaClientSecret) {
    throw new Error("Missing ALEXA_EVENT_CLIENT_ID or ALEXA_EVENT_CLIENT_SECRET");
  }

  const body = new URLSearchParams(values);
  const response = await fetch(lwaTokenUrl, {
    method: "POST",
    headers: {
      "content-type": "application/x-www-form-urlencoded"
    },
    body
  });

  const text = await response.text();
  const json = text ? JSON.parse(text) : {};

  if (!response.ok) {
    throw new Error(`LWA token request failed: ${response.status} ${text}`);
  }

  return json;
}

async function postAlexaEvent(url, body, accessToken) {
  const response = await fetch(url, {
    method: "POST",
    headers: {
      authorization: `Bearer ${accessToken}`,
      "content-type": "application/json"
    },
    body: JSON.stringify(body)
  });

  const responseText = await response.text();
  if (!response.ok) {
    throw new Error(`Alexa Event Gateway failed: ${response.status} ${responseText}`);
  }

  return { status: response.status, body: responseText };
}

async function getTokenRecord() {
  const result = await ddb.send(new GetItemCommand({
    TableName: tokenTable,
    Key: {
      id: { S: tokenRecordId }
    }
  }));

  const item = result.Item;
  if (!item) {
    return null;
  }

  return {
    accessToken: item.accessToken?.S || "",
    refreshToken: item.refreshToken?.S || "",
    expiresAt: item.expiresAt?.N || "0"
  };
}

async function saveTokens(tokenResponse) {
  const expiresInSeconds = Number(tokenResponse.expires_in || 3600);
  const expiresAt = Date.now() + expiresInSeconds * 1000;

  await ddb.send(new PutItemCommand({
    TableName: tokenTable,
    Item: {
      id: { S: tokenRecordId },
      accessToken: { S: tokenResponse.access_token || "" },
      refreshToken: { S: tokenResponse.refresh_token || "" },
      expiresAt: { N: String(expiresAt) }
    }
  }));
}

function alexaResponse(namespace, name) {
  return {
    event: {
      header: {
        namespace,
        name,
        messageId: crypto.randomUUID(),
        payloadVersion: "3"
      },
      payload: {}
    }
  };
}

function alexaErrorResponse(type, message) {
  return {
    event: {
      header: {
        namespace: "Alexa",
        name: "ErrorResponse",
        messageId: crypto.randomUUID(),
        payloadVersion: "3"
      },
      payload: {
        type,
        message
      }
    }
  };
}

function textLabel(text) {
  return {
    "@type": "text",
    value: {
      text,
      locale: "es-MX"
    }
  };
}

function normalizeEventId(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
}

function parseBody(event) {
  if (!event.body) {
    return {};
  }

  const raw = event.isBase64Encoded
    ? Buffer.from(event.body, "base64").toString("utf8")
    : event.body;

  return JSON.parse(raw);
}

function getBearerToken(headers) {
  const authorization = headers.authorization || headers.Authorization || "";
  const match = authorization.match(/^Bearer\s+(.+)$/i);
  return match ? match[1].trim() : "";
}

function httpResponse(statusCode, body) {
  return {
    statusCode,
    headers: {
      "content-type": "application/json"
    },
    body: JSON.stringify(body)
  };
}
