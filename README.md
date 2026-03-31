# MyLinuxBot

A Telegram bot for remotely controlling a Linux machine — run shell commands, chat with Gemini CLI, manage files, and automate HoneyComb trading.

## Features

| Command | Description |
|---------|-------------|
| `/shell <cmd>` | Run any shell command |
| `/ip` | Show local & public IP |
| `/uptime` | System uptime & RAM |
| `/screenshot` | Capture screen |
| `/ask <prompt>` | Ask Gemini CLI |
| `/gen <prompt>` | Generate code with Gemini |
| `/ls [path]` | List directory |
| `/cat <file>` | Read file content |
| `/upload <file>` | Send file to Telegram |
| `/trade` | Run HoneyComb trading bot |
| `/status` | View trade log |
| `/balance` | Check P&L |
| `/cron on/off` | Manage auto-trade schedule |

Plain text messages are forwarded to Gemini CLI as chat.  
Send a file to the bot to save it to `~/Downloads`.

## Setup

```bash
git clone https://github.com/YOUR_USERNAME/MyLinuxBot.git
cd MyLinuxBot
npm install
cp .env.example .env
```

Edit `.env`:
```
TELEGRAM_BOT_TOKEN=your_token_from_botfather
ALLOWED_CHAT_ID=your_chat_id
```

Get your Chat ID by messaging [@userinfobot](https://t.me/userinfobot) on Telegram.

## Run

```bash
npm start        # Normal
npm run dev      # Hot reload (development)
./run.sh         # Shell script
```

## Requirements

- Node.js 18+
- [Gemini CLI](https://github.com/google-gemini/gemini-cli) (for `/ask` and `/gen`)
- `scrot` (for `/screenshot`)

## Security

Only the configured `ALLOWED_CHAT_ID` can interact with the bot. All other requests are rejected.

## License

MIT
