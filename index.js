require('dotenv').config();
const { Telegraf } = require('telegraf');
const { authGuard } = require('./src/middleware');
const { registerSystemCommands } = require('./src/commands/system');
const { registerGeminiCommands } = require('./src/commands/gemini');
const { registerTradeCommands } = require('./src/commands/trade');
const { registerFileCommands } = require('./src/commands/files');

const token = process.env.TELEGRAM_BOT_TOKEN;
const allowedChatId = process.env.ALLOWED_CHAT_ID;

if (!token || token === 'your_bot_token_here') {
    console.error('Thieu TELEGRAM_BOT_TOKEN trong .env');
    process.exit(1);
}

const bot = new Telegraf(token);

bot.use(authGuard(allowedChatId));

bot.start((ctx) => {
    ctx.reply(
        `*MyLinuxBot — Remote Control*

*System*
/shell \`<cmd>\` — Chay lenh shell
/ip — Xem IP hien tai
/uptime — Uptime & RAM
/screenshot — Chup man hinh

*Gemini AI*
/ask \`<prompt>\` — Hoi Gemini
/gen \`<prompt>\` — Gemini tao code
_(Hoac gui tin nhan thuong de chat voi Gemini)_

*Files*
/ls \`[path]\` — Liet ke thu muc
/cat \`<file>\` — Doc file
/upload \`<file>\` — Gui file len Telegram
_(Gui file vao chat de luu vao ~/Downloads)_

*HoneyComb Trade*
/trade — Chay trading bot
/status — Xem trade log
/balance — Xem P&L
/cron — Quan ly auto-trade`,
        { parse_mode: 'Markdown' }
    );
});

registerSystemCommands(bot);
registerTradeCommands(bot);
registerFileCommands(bot);
registerGeminiCommands(bot);

bot.launch().then(() => {
    console.log('MyLinuxBot is running...');
    console.log(`Allowed Chat ID: ${allowedChatId}`);
});

process.once('SIGINT', () => bot.stop('SIGINT'));
process.once('SIGTERM', () => bot.stop('SIGTERM'));
