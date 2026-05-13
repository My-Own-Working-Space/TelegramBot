# MyLinuxBot (Autonomous AI Agent)

A high-performance Linux System Orchestrator built with .NET 9. This bot transforms your Linux machine into an autonomous agent capable of reasoning, job hunting, and secure system management via Telegram.

## Core Capabilities

### 1. Natural Language Orchestration
Powered by Groq AI (Llama 3.1 8B), the bot understands natural language requests. You can ask "How is my system?", "Any new jobs?", or "Take a screenshot" without remembering specific commands.

### 2. Multi-Site Job Scanner
Automated job discovery across premium platforms:
- ITviec, TopCV, and VietnamWorks.
- Intelligent filtering for Intern, Junior, and Fresher roles.
- Automatic notifications for new matching opportunities.
- SQLite-backed state tracking to prevent duplicate alerts.

### 3. Hardened Security Framework
Designed for secure homelab operation:
- Command Whitelisting: Only pre-defined binaries and regex-validated arguments are allowed.
- Path Isolation: Restrict file/log access to authorized directories only.
- Output & Timeout Control: Enforced 64KB output truncation and 30s execution timeouts.
- Loop Protection: AI agent reasoning is limited to 3 cycles per request to prevent cost/resource overrun.

## Technology Stack

- Framework: .NET 9 Worker Service
- AI Engine: Groq API (Llama 3.1 8B Instant)
- Browser Automation: Selenium WebDriver (Headless Chrome)
- Execution: CliWrap (Execv-style safe execution)
- Database: SQLite (Entity Framework Core)
- Containerization: Docker & Docker Compose

## Commands

- /scan: Immediate trigger for multi-site job scanning.
- /stats: Real-time system health summary (CPU, RAM, Disk).
- /screen: Captures and sends a host display screenshot.
- /power: System power management (Reboot/Shutdown).
- /shell <cmd>: Securely execute whitelisted system commands.

## Setup & Deployment

1. Configure your environment in .env:
   ```env
   TELEGRAM_BOT_TOKEN=your_token
   ALLOWED_CHAT_ID=your_id
   GROQ_API_KEY=your_groq_key
   ```

2. Customize security policies in security_config.json.

3. Deploy using Docker Compose:
   ```bash
   docker compose up -d --build
   ```

## Security Auditing
The system logs all execution attempts, blocked commands, and AI tool calls for auditing purposes. Unit and security tests are located in the MyLinuxBot.Tests project.
