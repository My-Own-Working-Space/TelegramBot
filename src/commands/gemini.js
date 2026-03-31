const { exec } = require('child_process');
const { truncate } = require('../utils');

const MAX_MSG_LEN = 4000;

function registerGeminiCommands(bot) {
    bot.command('ask', async (ctx) => {
        const prompt = ctx.message.text.replace('/ask', '').trim();
        if (!prompt) return ctx.reply('Cu phap: /ask <cau hoi>\nVi du: /ask Giai thich Docker la gi?');

        await ctx.reply('Dang hoi Gemini...');

        const escaped = prompt.replace(/'/g, "'\\''");
        exec(`echo '${escaped}' | gemini`, { timeout: 120000, maxBuffer: 2 * 1024 * 1024 }, (err, stdout, stderr) => {
            if (err) return ctx.reply(`Gemini loi: ${err.message}`);
            const output = truncate(stdout || stderr || 'Khong co phan hoi', MAX_MSG_LEN);
            ctx.reply(output);
        });
    });

    bot.command('gen', async (ctx) => {
        const prompt = ctx.message.text.replace('/gen', '').trim();
        if (!prompt) return ctx.reply('Cu phap: /gen <yeu cau>\nVi du: /gen Viet script Python doi ten file hang loat');

        await ctx.reply('Dang yeu cau Gemini tao code...');

        const escaped = prompt.replace(/'/g, "'\\''");
        const fullPrompt = `${escaped}. Hay chi tra ve code, khong giai thich them.`;
        exec(`echo '${fullPrompt}' | gemini`, { timeout: 120000, maxBuffer: 2 * 1024 * 1024 }, (err, stdout, stderr) => {
            if (err) return ctx.reply(`Gemini loi: ${err.message}`);
            const output = truncate(stdout || stderr || 'Khong co output', MAX_MSG_LEN);
            ctx.reply(output);
        });
    });

    bot.on('text', async (ctx) => {
        if (ctx.message.text.startsWith('/')) return;

        const prompt = ctx.message.text.trim();
        if (!prompt) return;

        await ctx.reply('Thinking...');

        const escaped = prompt.replace(/'/g, "'\\''");
        exec(`echo '${escaped}' | gemini`, { timeout: 120000, maxBuffer: 2 * 1024 * 1024 }, (err, stdout, stderr) => {
            if (err) return ctx.reply(`Loi: ${err.message}`);
            const output = truncate(stdout || stderr || 'Khong co phan hoi', MAX_MSG_LEN);
            ctx.reply(output);
        });
    });
}

module.exports = { registerGeminiCommands };
