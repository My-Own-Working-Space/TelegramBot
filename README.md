<<<<<<< HEAD
# MyLinuxBot (.NET 9 Edition)

A robust Telegram bot functioning as a system orchestrator for your Linux machine. Built with **.NET 9**, this application incorporates both a generic Worker Service (for long-polling Telegram messages) and a Minimal API WebHost (for receiving system Webhooks).

The current implementation utilizes **Clean Architecture**, the **Command Pattern**, and strict **Authorization Policies** to ensure secure interactions.

## Features & Commands

Currently migrated/implemented commands under the new .NET core:

| Command | Description |
|---------|-------------|
| `/shell <cmd>` | Securely executes any Bash shell command with timeouts. |
| `/k8s <cmd>` | Executes Kubernetes commands specifically bound to `~/Documents/Documents/K8s/Setup`. |
| `/ask <prompt>`| Sends natural language prompts to your local external Python script `gemini_cli.py`. |
| `/upload` | Whenever you upload a document in chat, it receives and locally saves it to `~/Downloads`. |
| `n8n Webhook`  | The bot runs a local Minimal API listening at `POST /webhook/n8n` to alert you of automated signals (e.g. trading alerts). |

### Security
- **Strict User Authorization**: The bot ignores all messages and updates that do not match the configured `ALLOWED_CHAT_ID`, guaranteeing that only the owner can trigger orchestrations.

---

## Code Architecture

- **Worker Service**: Uses generic `IHostedService` to listen for Telegram events transparently.
- **Dependency Injection**: Services (`IShellService`, `IGeminiService`), Commands, and the DB Context are modularly configured in the DI Container.
- **SQLite Database** (EF Core): Maintains entity sets for `ChatMessage` and `StoryState`. Automatically creates `MyLinuxBot.db` locally.

---

## Setup & Configuration

**1. Install Prerequisites:**
Ensure you have the .NET 9 SDK installed on your Linux machine.

**2. Clone the repository:**
```bash
git clone https://github.com/My-Own-Working-Space/TelegramBot.git
cd TelegramBot/MyLinuxBot
```

**3. Set Environment Variables:**
Create or edit the `.env` file within the `MyLinuxBot/` folder:
```env
TELEGRAM_BOT_TOKEN=your_token_from_botfather
ALLOWED_CHAT_ID=your_telegram_user_id
```

## Running the Bot

Navigate to the `.NET` application root folder and use the .NET CLI:

```bash
cd MyLinuxBot
dotnet build
dotnet run
```
_Optionally, you can utilize the included `Dockerfile` to build an optimized Linux Container._

## License

MIT
=======
# MyLinuxBot (Autonomous AI Agent Edition)

A high-performance, secure Linux System Orchestrator built with **.NET 9**. This bot transforms your Linux machine into an autonomous agent capable of reasoning, acting, and self-monitoring via Telegram.

## 🚀 Key Features

- **Autonomous AI Agent**: Integrated with Gemini CLI using a ReAct (Reasoning and Acting) logic. It can inspect CPU/RAM, read logs, and execute commands to solve your requests.
- **Security-First Architecture**: 
  - **Command Whitelist**: Only authorized binaries (ls, ps, df, etc.) are allowed to execute.
  - **Path Traversal Protection**: Secure log reading restricted to `/var/log/`.
  - **Shell Injection Guard**: Robust regex parsing and input sanitization.
- **Economical Mode**: Optimized for Gemini Free Tier to minimize API quota usage (1 request per message).
- **Remote Orchestration**: Manage system power (Reboot/Shutdown), take screenshots, and monitor processes from anywhere.
- **Voice Interaction**: Integrated with Whisper for voice-to-command transcription.

## 🛠️ Technology Stack

- **Core**: .NET 9 Worker Service & Minimal APIs
- **AI**: Google Gemini CLI (Reasoning) & OpenAI Whisper (Voice)
- **Execution**: CliWrap (Asynchronous shell execution)
- **Containerization**: Docker & Docker Compose (Privileged mode for system access)

## 📋 Commands

| Command | Description |
|---------|-------------|
| `/ask <prompt>` | Interact with the AI Agent to manage your system. |
| `/stats` | Quick summary of CPU, RAM, Disk, and Temperature. |
| `/top` | Lists top 10 processes by CPU usage. |
| `/power` | Interactive menu for Reboot, Shutdown, or Sleep. |
| `/screen` | Captures and sends a screenshot of the host's primary display. |

## ⚙️ Configuration

Create a `.env` file in the root directory:

```env
TELEGRAM_BOT_TOKEN=your_bot_token
ALLOWED_CHAT_ID=your_chat_id
GEMINI_API_KEY=your_google_ai_key
```

## 📦 Deployment

```bash
docker compose up -d --build
```

---
*Developed by **bunpmc** - Focused on Secure Linux Automation.*
>>>>>>> b5b6487 (Initial commit: Autonomous AI Linux Agent (Refactored))
