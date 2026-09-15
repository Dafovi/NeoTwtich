"""Neo Twitch adapter: upstream translation logic, input from EventSub, output over stdio.
The original bot is never connected to Twitch; no credentials cross this process boundary.
"""
import asyncio
import contextlib
import io
import json
import sys
from types import SimpleNamespace


async def main():
    settings = json.loads(sys.stdin.readline())
    with contextlib.redirect_stdout(io.StringIO()):
        import config
        config.TTS_In = config.TTS_Out = False
        config.Debug = False
        config.Show_ByName = config.Show_ByLang = False
        config.Translator = 'google'
        config.GAS_URL = ''
        config.GoogleTranslate_suffix = 'com'
        config.lang_TransToHome = settings['sourceLanguage'].split('-')[0]
        config.lang_HomeToOther = settings['targetLanguage']
        config.Ignore_Users = [x.strip().lower() for x in settings['ignoredUsers'].split(',') if x.strip()]
        import twitchTransFN as upstream

    async def no_commands(_):
        pass

    host = SimpleNamespace(handle_commands=no_commands)
    print(json.dumps({'kind': 'ready'}), flush=True)
    try:
        while True:
            line = await asyncio.to_thread(sys.stdin.readline)
            if not line:
                break
            message = json.loads(line)
            result = []

            async def capture(text):
                result.append(text.removeprefix('/me '))

            msg = SimpleNamespace(echo=False, content=message['text'],
                                  author=SimpleNamespace(name=message['user']), tags={},
                                  channel=SimpleNamespace(send=capture))
            try:
                with contextlib.redirect_stdout(io.StringIO()):
                    await asyncio.wait_for(upstream.Bot.event_message(host, msg), timeout=20)
                print(json.dumps({'kind': 'result', 'text': result[-1] if result else ''}, ensure_ascii=False), flush=True)
            except Exception:
                print(json.dumps({'kind': 'error', 'text': 'El servicio de traducción no respondió.'}), flush=True)
    finally:
        upstream.db.close()


asyncio.run(main())
