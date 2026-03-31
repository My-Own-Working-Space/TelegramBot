const { exec } = require('child_process');
const fs = require('fs');
const path = require('path');

const TRADE_SCRIPT = '/home/minhchau/Documents/MyTools/HCTrade/run.sh';
const TRADE_LOG = '/home/minhchau/.openclaw/workspace/trade_history.log';
const HCTRADE_DIR = '/home/minhchau/Documents/MyTools/HCTrade';

function formatTradeOutput(rawOutput) {
    const lines = rawOutput.split('\n');
    let result = '*Báo cáo giao dịch HoneyComb*\n\n';

    const scrapedLine = lines.find(l => l.includes('Scraped'));
    if (scrapedLine) {
        result += `${scrapedLine.trim()}\n\n`;
    }

    const commandsStart = rawOutput.indexOf('AI Commands:');
    if (commandsStart !== -1) {
        try {
            const jsonStr = rawOutput.slice(rawOutput.indexOf('[', commandsStart), rawOutput.indexOf(']', commandsStart) + 1);
            const commands = JSON.parse(jsonStr);

            result += '*AI Khuyến nghị:*\n\n';
            for (const cmd of commands) {
                const actionVi = cmd.action === 'BUY' ? 'MUA' : cmd.action === 'SHORT' ? 'BÁN KHỐNG' : cmd.action;
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
    result += isDryRun ? '\n_Chế độ DRY RUN — chưa thực hiện lệnh thật_' : '\n_Đã thực hiện lệnh giao dịch_';

    return result;
}

function registerTradeCommands(bot) {
    bot.command('trade', async (ctx) => {
        await ctx.reply('Đang chạy HoneyComb Trading Bot...\nVui lòng đợi khoảng 30 giây.');

        exec(`cd ${HCTRADE_DIR} && node index.js`, {
            timeout: 180000,
            maxBuffer: 2 * 1024 * 1024,
            env: { ...process.env, PATH: process.env.PATH }
        }, (err, stdout, stderr) => {
            const rawOutput = stdout || stderr || (err ? err.message : 'Không có đầu ra');

            try {
                const formatted = formatTradeOutput(rawOutput);
                ctx.reply(formatted, { parse_mode: 'Markdown' }).catch(() => {
                    ctx.reply(rawOutput.slice(-3500));
                });
            } catch (e) {
                ctx.reply(`Đầu ra thô:\n\`\`\`\n${rawOutput.slice(-3500)}\n\`\`\``, { parse_mode: 'Markdown' });
            }
        });
    });

    bot.command('status', async (ctx) => {
        if (!fs.existsSync(TRADE_LOG)) {
            return ctx.reply('Chưa có nhật ký giao dịch.');
        }

        exec(`tail -n 10 "${TRADE_LOG}"`, (err, stdout) => {
            if (err) return ctx.reply(`Lỗi: ${err.message}`);
            ctx.reply(`*Nhật ký (10 dòng cuối):*\n\`\`\`\n${stdout || 'Trống'}\n\`\`\``, { parse_mode: 'Markdown' });
        });
    });

    bot.command('balance', async (ctx) => {
        if (!fs.existsSync(TRADE_LOG)) {
            return ctx.reply('Chưa có dữ liệu giao dịch.');
        }

        exec(`grep -i "profit\\|loss\\|P&L\\|balance\\|HC" "${TRADE_LOG}" | tail -5`, (err, stdout) => {
            if (stdout && stdout.trim()) {
                ctx.reply(`*Báo cáo số dư:*\n\`\`\`\n${stdout}\n\`\`\``, { parse_mode: 'Markdown' });
            } else {
                ctx.reply('Chưa tìm thấy dữ liệu P&L trong nhật ký.');
            }
        });
    });
}

module.exports = { registerTradeCommands };
