# MyLinuxBot

A Telegram bot for remotely controlling a Linux machine — run shell commands, chat with Gemini CLI, manage files, and automate HoneyComb trading.

## Features

| Command | Description |
|---------|-------------|
| `/shell <cmd>` | Run any shell command |
| `/ip` | Show local & public IP |
| `/uptime` | System uptime & RAM |
| `/screenshot` | Capture screen |
| `/cron` | Manage cron jobs (AI-powered) |
| `/k8s <cmd>` | Manage Kubernetes clusters |
| `/ask <prompt>` | Ask Gemini CLI |
| `/gen <prompt>` | Generate code with Gemini |
| `/story <topic>` | Start an AI-written story (chapter by chapter) |
| `/ls [path]` | List directory |
| `/cat <file>` | Read file content |
| `/upload <file>` | Send file to Telegram |
| `/trade` | Run HoneyComb trading bot |
| `/status` | View trade log |
| `/balance` | Check P&L |

- **Natural Chat**: Plain text messages are forwarded to Gemini CLI as chat.
- **AI Story**: Start with `/story`. Say "chương tiếp" to continue the story.
- **K8s**: Commands run in `~/Documents/Documents/K8s/Setup`.
- **File Upload**: Send a file to the bot to save it to `~/Downloads`.

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

## Run

```bash
npm start        # Normal
npm run dev      # Hot reload (development)
./run.sh         # Shell script
```

## License

MIT
