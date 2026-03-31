const { exec } = require('child_process');
const { truncate } = require('../utils');

const MAX_MSG_LEN = 4000;

function registerGeminiCommands(bot) {
    bot.command('ask', async (ctx) => {
        const prompt = ctx.message.text.replace('/ask', '').trim();
        if (!prompt) return ctx.reply('Cú pháp: /ask <câu hỏi>\nVí dụ: /ask Giải thích Docker là gì?');

        ctx.sendChatAction('typing');

        const escaped = prompt.replace(/'/g, "'\\''");
        exec(`echo '${escaped}' | gemini`, { timeout: 120000, maxBuffer: 2 * 1024 * 1024 }, (err, stdout, stderr) => {
            if (err) return ctx.reply(`Lỗi Gemini: ${err.message}`);
            const output = truncate(stdout || stderr || 'Không có phản hồi', MAX_MSG_LEN);
            ctx.reply(output);
        });
    });

    bot.command('gen', async (ctx) => {
        const prompt = ctx.message.text.replace('/gen', '').trim();
        if (!prompt) return ctx.reply('Cú pháp: /gen <yêu cầu>\nVí dụ: /gen Viết script Python đổi tên tệp hàng loạt');

        ctx.sendChatAction('typing');

        const escaped = prompt.replace(/'/g, "'\\''");
        const fullPrompt = `${escaped}. Hãy chỉ trả về mã (code), không giải thích thêm.`;
        exec(`echo '${fullPrompt}' | gemini`, { timeout: 120000, maxBuffer: 2 * 1024 * 1024 }, (err, stdout, stderr) => {
            if (err) return ctx.reply(`Lỗi Gemini: ${err.message}`);
            const output = truncate(stdout || stderr || 'Không có đầu ra', MAX_MSG_LEN);
            ctx.reply(output);
        });
    });

    bot.on('text', async (ctx) => {
        if (ctx.message.text.startsWith('/')) return;

        const prompt = ctx.message.text.trim();
        if (!prompt) return;

        ctx.sendChatAction('typing');

        const escaped = prompt.replace(/'/g, "'\\''");
        exec(`echo '${escaped}' | gemini`, { timeout: 120000, maxBuffer: 2 * 1024 * 1024 }, (err, stdout, stderr) => {
            if (err) return ctx.reply(`Lỗi: ${err.message}`);
            const output = truncate(stdout || stderr || 'Không có phản hồi', MAX_MSG_LEN);
            ctx.reply(output);
        });
    });
}

module.exports = { registerGeminiCommands };
