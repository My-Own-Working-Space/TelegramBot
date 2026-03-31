const { exec } = require('child_process');
const fs = require('fs');
const path = require('path');

const TRADE_SCRIPT = '/home/minhchau/Documents/MyTools/HCTrade/run.sh';
const TRADE_LOG = '/home/minhchau/.openclaw/workspace/trade_history.log';
const HCTRADE_DIR = '/home/minhchau/Documents/MyTools/HCTrade';

function formatTradeOutput(rawOutput) {
    const lines = rawOutput.split('\n');
    let result = '*HoneyComb Trade Report*\n\n';

    const scrapedLine = lines.find(l => l.includes('Scraped'));
    if (scrapedLine) {
        result += `${scrapedLine.trim()}\n\n`;
    }

    const commandsStart = rawOutput.indexOf('AI Commands:');
    if (commandsStart !== -1) {
        try {
            const jsonStr = rawOutput.slice(rawOutput.indexOf('[', commandsStart), rawOutput.indexOf(']', commandsStart) + 1);
            const commands = JSON.parse(jsonStr);

            result += '*AI Khuyen Nghi:*\n\n';
            for (const cmd of commands) {
                const actionVi = cmd.action === 'BUY' ? 'MUA' : cmd.action === 'SHORT' ? 'BAN KHONG' : cmd.action;
                result += `[${actionVi}] \`${cmd.ticker}\`\n`;
                result += `  _${cmd.reason}_\n\n`;
            }
        } catch (e) {
            const cmdLines = lines.filter(l => l.includes('[TRADE]') || l.includes('[DRY RUN]'));
            for (const l of cmdLines) {
                result += `${l.trim()}\n`;
            }
        }
    }

    const isDryRun = rawOutput.includes('[DRY RUN]');
    result += isDryRun ? '\n_Che do DRY RUN — chua thuc hien lenh that_' : '\n_Da thuc hien lenh trade_';

    return result;
}

function registerTradeCommands(bot) {
    bot.command('trade', async (ctx) => {
        await ctx.reply('Dang chay HoneyComb Trading Bot...\nVui long doi ~30 giay.');

        exec(`cd ${HCTRADE_DIR} && node index.js`, {
            timeout: 180000,
            maxBuffer: 2 * 1024 * 1024,
            env: { ...process.env, PATH: process.env.PATH }
        }, (err, stdout, stderr) => {
            const rawOutput = stdout || stderr || (err ? err.message : 'Khong co output');

            try {
                const formatted = formatTradeOutput(rawOutput);
                ctx.reply(formatted, { parse_mode: 'Markdown' }).catch(() => {
                    ctx.reply(rawOutput.slice(-3500));
                });
            } catch (e) {
                ctx.reply(`Raw output:\n\`\`\`\n${rawOutput.slice(-3500)}\n\`\`\``, { parse_mode: 'Markdown' });
            }
        });
    });

    bot.command('status', async (ctx) => {
        if (!fs.existsSync(TRADE_LOG)) {
            return ctx.reply('Chua co trade log.');
        }

        exec(`tail -n 10 "${TRADE_LOG}"`, (err, stdout) => {
            if (err) return ctx.reply(`Loi: ${err.message}`);
            ctx.reply(`*Trade Log (10 dong cuoi):*\n\`\`\`\n${stdout || 'Trong'}\n\`\`\``, { parse_mode: 'Markdown' });
        });
    });

    bot.command('balance', async (ctx) => {
        if (!fs.existsSync(TRADE_LOG)) {
            return ctx.reply('Chua co du lieu giao dich.');
        }

        exec(`grep -i "profit\\|loss\\|P&L\\|balance\\|HC" "${TRADE_LOG}" | tail -5`, (err, stdout) => {
            if (stdout && stdout.trim()) {
                ctx.reply(`*Balance Report:*\n\`\`\`\n${stdout}\n\`\`\``, { parse_mode: 'Markdown' });
            } else {
                ctx.reply('Chua co du lieu P&L trong log.');
            }
        });
    });

    bot.command('cron', async (ctx) => {
        const arg = ctx.message.text.replace('/cron', '').trim().toLowerCase();
        const cronJob = `0 */4 * * * cd ${HCTRADE_DIR} && node index.js >> /home/minhchau/.openclaw/workspace/trade_history.log 2>&1`;
        const cronComment = '# MyLinuxBot HCTrade Auto';

        if (arg === 'on') {
            exec(`(crontab -l 2>/dev/null | grep -v "HCTrade Auto"; echo "${cronComment}"; echo "${cronJob}") | crontab -`, (err) => {
                if (err) return ctx.reply(`Loi: ${err.message}`);
                ctx.reply('*Cron da BAT!*\nBot se tu dong trade moi 4 gio.\n\nDung /cron off de tat.', { parse_mode: 'Markdown' });
            });
        } else if (arg === 'off') {
            exec(`crontab -l 2>/dev/null | grep -v "HCTrade Auto" | grep -v "${HCTRADE_DIR}" | crontab -`, (err) => {
                if (err) return ctx.reply(`Loi: ${err.message}`);
                ctx.reply('*Cron da TAT!*\nBot se khong tu dong trade nua.', { parse_mode: 'Markdown' });
            });
        } else {
            exec('crontab -l 2>/dev/null', (err, stdout) => {
                const hasJob = stdout && stdout.includes('HCTrade');
                const status = hasJob ? '*DANG BAT*' : '*DANG TAT*';
                let msg = `*Cron Status:* ${status}\n\n`;
                msg += '*Cach dung:*\n';
                msg += '/cron on — Bat auto-trade moi 4h\n';
                msg += '/cron off — Tat auto-trade\n';
                if (hasJob) {
                    msg += `\n*Cron hien tai:*\n\`\`\`\n${stdout}\n\`\`\``;
                }
                ctx.reply(msg, { parse_mode: 'Markdown' });
            });
        }
    });
}

module.exports = { registerTradeCommands };
