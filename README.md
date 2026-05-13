# MyLinuxBot (Autonomous AI Agent Edition)

A high-performance, secure Linux System Orchestrator built with **.NET 9**. This bot transforms your Linux machine into an autonomous agent capable of reasoning, acting, and self-monitoring via Telegram.

## Key Features

- **Autonomous AI Agent**: Integrated with Gemini CLI using a ReAct (Reasoning and Acting) logic. It can inspect CPU/RAM, read logs, and execute commands to solve your requests.
- **Security-First Architecture**: 
  - **Command Whitelist**: Only authorized binaries (ls, ps, df, etc.) are allowed to execute.
  - **Path Traversal Protection**: Secure log reading restricted to `/var/log/`.
  - **Shell Injection Guard**: Robust regex parsing and input sanitization.
- **Economical Mode**: Optimized for Gemini Free Tier to minimize API quota usage (1 request per message).
- **Remote Orchestration**: Manage system power (Reboot/Shutdown), take screenshots, and monitor processes from anywhere.
- **Voice Interaction**: Integrated with Whisper for voice-to-command transcription.

## Technology Stack

- **Core**: .NET 9 Worker Service & Minimal APIs
- **AI**: Google Gemini CLI (Reasoning) & OpenAI Whisper (Voice)
- **Execution**: CliWrap (Asynchronous shell execution)
- **Containerization**: Docker & Docker Compose (Privileged mode for system access)

## Commands

| Command | Description |
|---------|-------------|
| `/ask <prompt>` | Interact with the AI Agent to manage your system. |
| `/stats` | Quick summary of CPU, RAM, Disk, and Temperature. |
| `/top` | Lists top 10 processes by CPU usage. |
| `/power` | Interactive menu for Reboot, Shutdown, or Sleep. |
| `/screen` | Captures and sends a screenshot of the host's primary display. |

## Configuration

Create a `.env` file in the root directory:

```env
TELEGRAM_BOT_TOKEN=your_bot_token
ALLOWED_CHAT_ID=your_chat_id
GEMINI_API_KEY=your_google_ai_key
```

## Deployment

```bash
docker compose up -d --build
```
