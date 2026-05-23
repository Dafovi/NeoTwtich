#include <Adafruit_NeoPixel.h>

#define MAX_STRIPS 6
#define MAX_LEDS_PER_STRIP 300
#define SERIAL_BAUD 115200

struct LedTarget {
  int pin;
  int ledCount;
};

struct RuntimeStrip {
  bool active;
  int pin;
  int ledCount;
  Adafruit_NeoPixel pixels;
};

struct EffectCommand {
  LedTarget targets[MAX_STRIPS];
  int targetCount;
  String pattern;
  int brightness;
  unsigned long durationMs;
  unsigned long cycleMs;
  unsigned long stepMs;
  uint32_t primary;
  uint32_t secondary;
  uint32_t tertiary;
};

RuntimeStrip strips[MAX_STRIPS];

void setup() {
  Serial.begin(SERIAL_BAUD);
  Serial.setTimeout(30);
  randomSeed(analogRead(A0));
}

void loop() {
  if (!Serial.available()) {
    return;
  }

  String line = Serial.readStringUntil('\n');
  line.trim();

  if (line.startsWith("STOP|")) {
    LedTarget targets[MAX_STRIPS];
    int targetCount = parseTargets(line.substring(5), targets);
    clearTargets(targets, targetCount);
    return;
  }

  EffectCommand command;
  if (!parseCommand(line, command)) {
    return;
  }

  prepareTargets(command);
  bool stopped = runEffect(command);

  if (!stopped && command.durationMs > 0) {
    clearAll(command);
  }
}

bool parseCommand(String line, EffectCommand &command) {
  String parts[10];
  int start = 0;

  for (int i = 0; i < 10; i++) {
    int separator = line.indexOf('|', start);
    if (separator < 0 && i < 9) {
      return false;
    }

    if (i == 9) {
      parts[i] = line.substring(start);
    } else {
      parts[i] = line.substring(start, separator);
      start = separator + 1;
    }
  }

  if (parts[0] != "FX") {
    return false;
  }

  command.targetCount = parseTargets(parts[1], command.targets);
  if (command.targetCount == 0) {
    return false;
  }

  command.pattern = parts[2];
  command.brightness = constrain(parts[3].toInt(), 0, 255);
  command.durationMs = (unsigned long)parts[4].toInt();
  command.cycleMs = max((unsigned long)10, (unsigned long)parts[5].toInt());
  command.stepMs = max((unsigned long)10, (unsigned long)parts[6].toInt());
  command.primary = parseColor(parts[7]);
  command.secondary = parseColor(parts[8]);
  command.tertiary = parseColor(parts[9]);

  return true;
}

int parseTargets(String value, LedTarget targets[]) {
  int count = 0;
  int start = 0;

  while (count < MAX_STRIPS && start < value.length()) {
    int separator = value.indexOf(',', start);
    String token = separator < 0 ? value.substring(start) : value.substring(start, separator);
    token.trim();

    int colon = token.indexOf(':');
    if (colon > 0) {
      int pin = token.substring(0, colon).toInt();
      int leds = token.substring(colon + 1).toInt();

      if (pin >= 0 && leds > 0) {
        targets[count].pin = pin;
        targets[count].ledCount = constrain(leds, 1, MAX_LEDS_PER_STRIP);
        count++;
      }
    }

    if (separator < 0) {
      break;
    }

    start = separator + 1;
  }

  return count;
}

uint32_t parseColor(String value) {
  value.trim();
  if (value.startsWith("#")) {
    value.remove(0, 1);
  }

  if (value.length() != 6) {
    return packColor(255, 255, 255);
  }

  long rgb = strtol(value.c_str(), NULL, 16);
  byte red = (rgb >> 16) & 0xFF;
  byte green = (rgb >> 8) & 0xFF;
  byte blue = rgb & 0xFF;

  return packColor(red, green, blue);
}

uint32_t packColor(byte red, byte green, byte blue) {
  return ((uint32_t)red << 16) | ((uint32_t)green << 8) | blue;
}

void prepareTargets(EffectCommand &command) {
  for (int i = 0; i < command.targetCount; i++) {
    RuntimeStrip *runtime = getRuntimeStrip(command.targets[i].pin, command.targets[i].ledCount);
    if (runtime == NULL) {
      continue;
    }

    runtime->pixels.setBrightness(command.brightness);
    runtime->pixels.clear();
    runtime->pixels.show();
  }
}

RuntimeStrip *getRuntimeStrip(int pin, int ledCount) {
  for (int i = 0; i < MAX_STRIPS; i++) {
    if (strips[i].active && strips[i].pin == pin) {
      if (strips[i].ledCount != ledCount) {
        strips[i].pixels.updateLength(ledCount);
        strips[i].ledCount = ledCount;
        strips[i].pixels.begin();
      }

      return &strips[i];
    }
  }

  for (int i = 0; i < MAX_STRIPS; i++) {
    if (!strips[i].active) {
      strips[i].active = true;
      strips[i].pin = pin;
      strips[i].ledCount = ledCount;
      strips[i].pixels.updateType(NEO_GRB + NEO_KHZ800);
      strips[i].pixels.updateLength(ledCount);
      strips[i].pixels.setPin(pin);
      strips[i].pixels.begin();
      return &strips[i];
    }
  }

  return NULL;
}

bool runEffect(EffectCommand command) {
  if (command.pattern == "SOLID") {
    return solid(command);
  } else if (command.pattern == "PULSE") {
    return pulse(command);
  } else if (command.pattern == "RAINBOW") {
    return rainbow(command);
  } else if (command.pattern == "CHASE") {
    return chase(command);
  } else if (command.pattern == "THEATER") {
    return theater(command);
  } else if (command.pattern == "SPARKLE") {
    return sparkle(command);
  } else if (command.pattern == "RAVE") {
    return rave(command);
  }

  return solid(command);
}

bool isRunning(EffectCommand command, unsigned long startedAt) {
  return command.durationMs == 0 || millis() - startedAt < command.durationMs;
}

bool waitOrStop(unsigned long waitMs, EffectCommand command) {
  unsigned long startedAt = millis();
  while (millis() - startedAt < waitMs) {
    if (Serial.available()) {
      String line = Serial.readStringUntil('\n');
      line.trim();

      if (line.startsWith("STOP|")) {
        clearAll(command);
        return true;
      }
    }

    delay(5);
  }

  return false;
}

bool solid(EffectCommand command) {
  unsigned long startedAt = millis();
  fillAll(command, command.primary);

  while (isRunning(command, startedAt)) {
    if (waitOrStop(command.stepMs, command)) {
      return true;
    }
  }

  return false;
}

bool pulse(EffectCommand command) {
  unsigned long startedAt = millis();

  while (isRunning(command, startedAt)) {
    for (int level = 20; level <= command.brightness && isRunning(command, startedAt); level += 8) {
      setBrightnessAll(command, level);
      fillAll(command, command.primary);
      if (waitOrStop(command.cycleMs, command)) {
        return true;
      }
    }

    for (int level = command.brightness; level >= 20 && isRunning(command, startedAt); level -= 8) {
      setBrightnessAll(command, level);
      fillAll(command, command.secondary);
      if (waitOrStop(command.cycleMs, command)) {
        return true;
      }
    }
  }

  setBrightnessAll(command, command.brightness);
  return false;
}

bool rainbow(EffectCommand command) {
  unsigned long startedAt = millis();
  uint16_t offset = 0;

  while (isRunning(command, startedAt)) {
    for (int target = 0; target < command.targetCount; target++) {
      RuntimeStrip *runtime = getRuntimeStrip(command.targets[target].pin, command.targets[target].ledCount);
      if (runtime == NULL) {
        continue;
      }

      for (int pixel = 0; pixel < runtime->ledCount; pixel++) {
        uint16_t hue = offset + (pixel * 65535L / runtime->ledCount);
        runtime->pixels.setPixelColor(pixel, runtime->pixels.gamma32(runtime->pixels.ColorHSV(hue)));
      }

      runtime->pixels.show();
    }

    offset += 256;
    if (waitOrStop(command.cycleMs, command)) {
      return true;
    }
  }

  return false;
}

bool chase(EffectCommand command) {
  unsigned long startedAt = millis();
  int head = 0;

  while (isRunning(command, startedAt)) {
    for (int target = 0; target < command.targetCount; target++) {
      RuntimeStrip *runtime = getRuntimeStrip(command.targets[target].pin, command.targets[target].ledCount);
      if (runtime == NULL) {
        continue;
      }

      for (int pixel = 0; pixel < runtime->ledCount; pixel++) {
        runtime->pixels.setPixelColor(pixel, command.secondary);
      }

      for (int tail = 0; tail < 4; tail++) {
        int pixel = (head - tail + runtime->ledCount) % runtime->ledCount;
        runtime->pixels.setPixelColor(pixel, pickColor(command, tail));
      }

      runtime->pixels.show();
    }

    head++;
    if (waitOrStop(command.cycleMs, command)) {
      return true;
    }
  }

  return false;
}

bool theater(EffectCommand command) {
  unsigned long startedAt = millis();
  int phase = 0;

  while (isRunning(command, startedAt)) {
    for (int target = 0; target < command.targetCount; target++) {
      RuntimeStrip *runtime = getRuntimeStrip(command.targets[target].pin, command.targets[target].ledCount);
      if (runtime == NULL) {
        continue;
      }

      for (int pixel = 0; pixel < runtime->ledCount; pixel++) {
        int lane = (pixel + phase) % 3;
        runtime->pixels.setPixelColor(pixel, pickColor(command, lane));
      }

      runtime->pixels.show();
    }

    phase = (phase + 1) % 3;
    if (waitOrStop(command.cycleMs, command)) {
      return true;
    }
  }

  return false;
}

bool sparkle(EffectCommand command) {
  unsigned long startedAt = millis();
  clearAll(command);

  while (isRunning(command, startedAt)) {
    for (int target = 0; target < command.targetCount; target++) {
      RuntimeStrip *runtime = getRuntimeStrip(command.targets[target].pin, command.targets[target].ledCount);
      if (runtime == NULL) {
        continue;
      }

      int pixel = random(runtime->ledCount);
      runtime->pixels.setPixelColor(pixel, pickColor(command, random(3)));
      runtime->pixels.show();
    }

    if (waitOrStop(command.stepMs, command)) {
      return true;
    }

    clearAll(command);

    if (waitOrStop(command.cycleMs, command)) {
      return true;
    }
  }

  return false;
}

bool rave(EffectCommand command) {
  unsigned long startedAt = millis();

  while (isRunning(command, startedAt)) {
    for (int target = 0; target < command.targetCount; target++) {
      RuntimeStrip *runtime = getRuntimeStrip(command.targets[target].pin, command.targets[target].ledCount);
      if (runtime == NULL) {
        continue;
      }

      runtime->pixels.setBrightness(random(max(1, command.brightness / 3), max(2, command.brightness + 1)));

      for (int pixel = 0; pixel < runtime->ledCount; pixel++) {
        bool lit = random(100) > 35;
        runtime->pixels.setPixelColor(pixel, lit ? pickColor(command, random(3)) : 0);
      }

      runtime->pixels.show();
    }

    if (waitOrStop(command.stepMs, command)) {
      return true;
    }

    if (random(100) > 45) {
      clearAll(command);
      if (waitOrStop(command.cycleMs, command)) {
        return true;
      }
    }
  }

  setBrightnessAll(command, command.brightness);
  return false;
}

uint32_t pickColor(EffectCommand command, int index) {
  if (index % 3 == 0) {
    return command.primary;
  }

  if (index % 3 == 1) {
    return command.secondary;
  }

  return command.tertiary;
}

void setBrightnessAll(EffectCommand command, int brightness) {
  for (int target = 0; target < command.targetCount; target++) {
    RuntimeStrip *runtime = getRuntimeStrip(command.targets[target].pin, command.targets[target].ledCount);
    if (runtime == NULL) {
      continue;
    }

    runtime->pixels.setBrightness(brightness);
  }
}

void fillAll(EffectCommand command, uint32_t color) {
  for (int target = 0; target < command.targetCount; target++) {
    RuntimeStrip *runtime = getRuntimeStrip(command.targets[target].pin, command.targets[target].ledCount);
    if (runtime == NULL) {
      continue;
    }

    for (int pixel = 0; pixel < runtime->ledCount; pixel++) {
      runtime->pixels.setPixelColor(pixel, color);
    }

    runtime->pixels.show();
  }
}

void clearTargets(LedTarget targets[], int targetCount) {
  for (int target = 0; target < targetCount; target++) {
    RuntimeStrip *runtime = getRuntimeStrip(targets[target].pin, targets[target].ledCount);
    if (runtime == NULL) {
      continue;
    }

    runtime->pixels.clear();
    runtime->pixels.show();
  }
}

void clearAll(EffectCommand command) {
  clearTargets(command.targets, command.targetCount);
}
