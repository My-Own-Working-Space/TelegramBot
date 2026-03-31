const { exec } = require('child_process');
const { truncate } = require('../utils');

const MAX_MSG_LEN = 4000;

function registerSystemCommands(bot) {
    bot.command('shell', async (ctx) => {
        const cmd = ctx.message.text.replace('/shell', '').trim();
        if (!cmd) return ctx.reply('Cu phap: /shell <lenh>\nVi du: /shell ls -la');

        await ctx.reply(`Dang chay: \`${cmd}\`...`, { parse_mode: 'Markdown' });

        exec(cmd, { timeout: 30000, maxBuffer: 1024 * 1024 }, (err, stdout, stderr) => {
            const output = stdout || stderr || (err ? err.message : 'Khong co output');
            const result = truncate(output, MAX_MSG_LEN);
            ctx.reply(`\`\`\`\n${result}\n\`\`\``, { parse_mode: 'Markdown' }).catch(() => {
                ctx.reply(result);
            });
        });
    });

    bot.command('ip', (ctx) => {
        exec('hostname -I && curl -s ifconfig.me', { timeout: 10000 }, (err, stdout) => {
            const lines = (stdout || 'Khong lay duoc IP').trim().split('\n');
            const localIP = lines[0]?.trim() || 'N/A';
            const publicIP = lines[1]?.trim() || 'N/A';
            ctx.reply(`*IP Info*\nLocal: \`${localIP}\`\nPublic: \`${publicIP}\``, { parse_mode: 'Markdown' });
        });
    });

    bot.command('uptime', (ctx) => {
        exec('uptime -p && free -h | grep Mem', { timeout: 5000 }, (err, stdout) => {
            ctx.reply(`*System*\n\`\`\`\n${stdout || 'N/A'}\n\`\`\``, { parse_mode: 'Markdown' });
        });
    });

    bot.command('screenshot', async (ctx) => {
        await ctx.reply('Dang chup man hinh...');
        const screenshotPath = '/tmp/telegram_screenshot.png';
        exec(`scrot ${screenshotPath} -o`, { timeout: 10000 }, async (err) => {
            if (err) return ctx.reply(`Loi: ${err.message}`);
            try {
                await ctx.replyWithPhoto({ source: screenshotPath });
            } catch (e) {
                ctx.reply(`Khong gui duoc anh: ${e.message}`);
            }
        });
    });
}

module.exports = { registerSystemCommands };
