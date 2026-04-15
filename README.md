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
