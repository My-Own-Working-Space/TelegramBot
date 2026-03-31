require('dotenv').config();
const { Telegraf } = require('telegraf');
const { authGuard } = require('./src/middleware');
const { registerSystemCommands } = require('./src/commands/system');
const { registerGeminiCommands } = require('./src/commands/gemini');
const { registerTradeCommands } = require('./src/commands/trade');
const { registerFileCommands } = require('./src/commands/files');
const { registerStoryCommands } = require('./src/commands/story');
const { registerK8sCommands } = require('./src/commands/k8s');

const token = process.env.TELEGRAM_BOT_TOKEN;
const allowedChatId = process.env.ALLOWED_CHAT_ID;

if (!token || token === 'your_bot_token_here') {
    console.error('Thiếu TELEGRAM_BOT_TOKEN trong .env');
    process.exit(1);
}

const bot = new Telegraf(token);

// Security: Only allow configured Chat ID
bot.use(authGuard(allowedChatId));

// /start — Welcome message with command list
bot.start((ctx) => {
    ctx.reply(
        `*MyLinuxBot — Điều khiển từ xa*

*Hệ thống*
/shell \`<lệnh>\` — Chạy lệnh shell
/ip — Xem IP
/uptime — Thời gian hoạt động & RAM
/screenshot — Chụp màn hình
/cron — Quản lý lịch trình (AI)
/k8s \`<lệnh>\` — Quản lý Kubernetes

*Gemini AI*
/ask \`<yêu cầu>\` — Hỏi Gemini
/gen \`<yêu cầu>\` — Gemini tạo mã
/story \`<chủ đề>\` — Viết truyện AI (theo chương)
_(Hoặc nhắn tin thường để chat trực tiếp)_

*Tệp tin*
/ls \`[đường dẫn]\` — Liệt kê thư mục
/cat \`<tệp>\` — Đọc nội dung tệp
/upload \`<tệp>\` — Gửi tệp lên Telegram

*Giao dịch HoneyComb*
/trade — Chạy bot giao dịch
/status — Xem nhật ký giao dịch
/balance — Xem số dư & Lãi/Lỗ`,
        { parse_mode: 'Markdown' }
    );
});

// Register all command modules
registerSystemCommands(bot);
registerTradeCommands(bot);
registerFileCommands(bot);
registerStoryCommands(bot);
registerK8sCommands(bot);
registerGeminiCommands(bot);

// Launch
bot.launch().then(() => {
    console.log('MyLinuxBot đang hoạt động...');
    console.log(`Chat ID được phép: ${allowedChatId}`);
});

// Graceful stop
process.once('SIGINT', () => bot.stop('SIGINT'));
process.once('SIGTERM', () => bot.stop('SIGTERM'));
