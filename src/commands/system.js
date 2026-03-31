const { exec } = require('child_process');
const { truncate } = require('../utils');

const MAX_MSG_LEN = 4000;

function registerSystemCommands(bot) {
    bot.command('shell', async (ctx) => {
        const cmd = ctx.message.text.replace('/shell', '').trim();
        if (!cmd) return ctx.reply('Cú pháp: /shell <lệnh>\nVí dụ: /shell ls -la');

        await ctx.reply(`Đang chạy: \`${cmd}\`...`, { parse_mode: 'Markdown' });

        exec(cmd, { timeout: 30000, maxBuffer: 1024 * 1024 }, (err, stdout, stderr) => {
            const output = stdout || stderr || (err ? err.message : 'Không có đầu ra');
            const result = truncate(output, MAX_MSG_LEN);
            ctx.reply(`\`\`\`\n${result}\n\`\`\``, { parse_mode: 'Markdown' }).catch(() => {
                ctx.reply(result);
            });
        });
    });

    bot.command('ip', (ctx) => {
        exec('hostname -I && curl -s ifconfig.me', { timeout: 10000 }, (err, stdout) => {
            const lines = (stdout || 'Không lấy được IP').trim().split('\n');
            const localIP = lines[0]?.trim() || 'N/A';
            const publicIP = lines[1]?.trim() || 'N/A';
            ctx.reply(`*Thông tin IP*\nLocal: \`${localIP}\`\nPublic: \`${publicIP}\``, { parse_mode: 'Markdown' });
        });
    });

    bot.command('uptime', (ctx) => {
        exec('uptime -p && free -h | grep Mem', { timeout: 5000 }, (err, stdout) => {
            ctx.reply(`*Hệ thống*\n\`\`\`\n${stdout || 'N/A'}\n\`\`\``, { parse_mode: 'Markdown' });
        });
    });

    bot.command('screenshot', async (ctx) => {
        await ctx.reply('Đang chụp màn hình...');
        const screenshotPath = '/tmp/telegram_screenshot.png';
        exec(`scrot ${screenshotPath} -o`, { timeout: 10000 }, async (err) => {
            if (err) return ctx.reply(`Lỗi: ${err.message}`);
            try {
                await ctx.replyWithPhoto({ source: screenshotPath });
            } catch (e) {
                ctx.reply(`Không gửi được ảnh: ${e.message}`);
            }
        });
    });

    bot.command('cron', async (ctx) => {
        const arg = ctx.message.text.replace('/cron', '').trim();

        if (!arg || arg === 'list') {
            exec('crontab -l 2>/dev/null', (err, stdout) => {
                if (!stdout || !stdout.trim()) {
                    return ctx.reply('Chưa có tác vụ lập lịch nào.');
                }
                const lines = stdout.trim().split('\n');
                let msg = '*Danh sách tác vụ lập lịch (Cron):*\n\n';
                lines.forEach((line, i) => {
                    if (line.startsWith('#')) {
                        msg += `_${line}_\n`;
                    } else {
                        msg += `\`${i + 1}.\` \`${line}\`\n`;
                    }
                });
                msg += '\nDùng /cron del <số> để xóa\nDùng /cron <yêu cầu> để tạo mới';
                ctx.reply(msg, { parse_mode: 'Markdown' });
            });
            return;
        }

        if (arg.startsWith('del ')) {
            const num = parseInt(arg.replace('del', '').trim());
            if (isNaN(num)) return ctx.reply('Cú pháp: /cron del <số dòng>');

            exec('crontab -l 2>/dev/null', (err, stdout) => {
                if (!stdout) return ctx.reply('Không có tác vụ nào để xóa.');
                const lines = stdout.trim().split('\n');
                if (num < 1 || num > lines.length) return ctx.reply(`Số dòng không hợp lệ (1-${lines.length})`);

                lines.splice(num - 1, 1);
                const newCron = lines.join('\n') + '\n';
                exec(`echo '${newCron}' | crontab -`, (err2) => {
                    if (err2) return ctx.reply(`Lỗi: ${err2.message}`);
                    ctx.reply(`Đã xóa tác vụ tại dòng ${num}.`);
                });
            });
            return;
        }

        await ctx.reply('Đang phân tích yêu cầu bằng AI...');

        const prompt = `Bạn là một sysadmin Linux. Người dùng yêu cầu tạo cron job: "${arg}"
Hãy trả về CHÍNH XÁC 1 dòng cron job hợp lệ, không giải thích gì thêm.
Ví dụ: 0 */4 * * * /path/to/script.sh
CHỈ TRẢ VỀ 1 DÒNG CRON, KHÔNG CÓ GÌ KHÁC.`;

        const escaped = prompt.replace(/'/g, "'\\''");
        exec(`echo '${escaped}' | gemini`, { timeout: 60000, maxBuffer: 1024 * 1024 }, (err, stdout) => {
            if (err) return ctx.reply(`Lỗi Gemini: ${err.message}`);

            const lines = (stdout || '').trim().split('\n');
            const cronLine = lines.find(l => {
                const clean = l.replace(/^```\w*/, '').replace(/```$/, '').trim();
                return clean && /^[\d\*\/\-\,]+\s/.test(clean);
            });

            if (!cronLine) {
                return ctx.reply(`AI không tạo được dòng lệnh cron phù hợp.\nĐầu ra: ${truncate(stdout, 500)}`);
            }

            const cleanCron = cronLine.replace(/^```\w*/, '').replace(/```$/, '').trim();

            ctx.reply(
                `*Tác vụ sẽ được tạo:*\n\`${cleanCron}\`\n\nGửi "ok" để xác nhận, hoặc "huy" để hủy.`,
                { parse_mode: 'Markdown' }
            );

            const listener = async (confirmCtx) => {
                const reply = confirmCtx.message.text.toLowerCase().trim();
                if (reply === 'ok' || reply === 'yes' || reply === 'y') {
                    exec(`(crontab -l 2>/dev/null; echo "${cleanCron}") | crontab -`, (err2) => {
                        if (err2) return confirmCtx.reply(`Lỗi: ${err2.message}`);
                        confirmCtx.reply(`Đã tạo tác vụ:\n\`${cleanCron}\`\n\nDùng /cron list để xem lại.`, { parse_mode: 'Markdown' });
                    });
                } else {
                    confirmCtx.reply('Đã hủy yêu cầu.');
                }
                bot.off('text', listener);
            };

            bot.on('text', listener);
            setTimeout(() => bot.off('text', listener), 30000);
        });
    });
}

module.exports = { registerSystemCommands };
