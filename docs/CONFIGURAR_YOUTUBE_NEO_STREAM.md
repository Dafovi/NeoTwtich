# Configurar YouTube en Neo Stream

Neo Stream se conectará a YouTube como una aplicación de escritorio. No usa una contraseña de
Google ni un `client secret`: abre el navegador oficial de Google y recibe un permiso revocable
mediante OAuth 2.0 con PKCE.

## Lo que tendrás que crear

1. Abre [Google Cloud Console](https://console.cloud.google.com/) e inicia sesión con la cuenta
   propietaria del proyecto.
2. Crea un proyecto, por ejemplo `Neo Stream`, o selecciona uno existente.
3. Ve a **APIs y servicios > Biblioteca**, busca **YouTube Data API v3** y pulsa **Habilitar**.
4. En **APIs y servicios > Pantalla de consentimiento OAuth**, elige **Externo**. Mientras el
   proyecto esté en pruebas, agrega tu cuenta de Google en **Usuarios de prueba**.
5. En **Credenciales**, crea un **ID de cliente OAuth** de tipo **Aplicación de escritorio**.
   Dale un nombre descriptivo, por ejemplo `Neo Stream Desktop`.
6. Copia solamente el **Client ID**, que termina en `.apps.googleusercontent.com`.

No copies ni compartas tokens de acceso. Una aplicación de escritorio pública no necesita ni debe
guardar un Client Secret para este flujo.

## Qué hará la aplicación

Al pulsar conectar, Neo Stream abrirá Google en el navegador. Tras aprobar los permisos, Google
vuelve a una dirección temporal `http://127.0.0.1:<puerto>/` que existe solo durante esa conexión.
La aplicación intercambia el código con PKCE, guarda los tokens con DPAPI de Windows y puede
renovarlos sin volver a pedir autorización hasta que revoques el acceso desde tu cuenta de Google.

La primera integración consulta el directo activo y su chat. Las alertas, comandos de chat y la
mezcla de plataformas se conectarán sobre el contrato común ya existente, sin cambiar las reglas de
Twitch que ya funcionen.
