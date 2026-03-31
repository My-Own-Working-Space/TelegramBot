const { exec } = require('child_process');
const fs = require('fs');
const path = require('path');
const { truncate } = require('../utils');

function registerFileCommands(bot) {
    bot.command('ls', (ctx) => {
        const dir = ctx.message.text.replace('/ls', '').trim() || '/home/minhchau';
        exec(`ls -lah "${dir}"`, { timeout: 5000 }, (err, stdout) => {
            if (err) return ctx.reply(`Lỗi: ${err.message}`);
            ctx.reply(`\`${dir}\`\n\`\`\`\n${truncate(stdout, 3500)}\n\`\`\``, { parse_mode: 'Markdown' });
        });
    });

    bot.command('cat', (ctx) => {
        const filePath = ctx.message.text.replace('/cat', '').trim();
        if (!filePath) return ctx.reply('Cú pháp: /cat <đường_dẫn_tệp>');

        try {
            if (!fs.existsSync(filePath)) return ctx.reply('Tệp không tồn tại.');
            const stat = fs.statSync(filePath);
            if (stat.size > 50000) return ctx.reply('Tệp quá lớn (>50KB). Dùng /upload để tải về máy.');

            const content = fs.readFileSync(filePath, 'utf-8');
            ctx.reply(`\`${filePath}\`\n\`\`\`\n${truncate(content, 3500)}\n\`\`\``, { parse_mode: 'Markdown' }).catch(() => {
                ctx.reply(`${filePath}\n${truncate(content, 3500)}`);
            });
        } catch (e) {
            ctx.reply(`Lỗi: ${e.message}`);
        }
    });

    bot.command('upload', async (ctx) => {
        const filePath = ctx.message.text.replace('/upload', '').trim();
        if (!filePath) return ctx.reply('Cú pháp: /upload <đường_dẫn_tệp>');

        try {
            if (!fs.existsSync(filePath)) return ctx.reply('Tệp không tồn tại.');
            await ctx.replyWithDocument({ source: filePath, filename: path.basename(filePath) });
        } catch (e) {
            ctx.reply(`Không gửi được: ${e.message}`);
        }
    });

    bot.on('document', async (ctx) => {
        try {
            const file = ctx.message.document;
            const link = await ctx.telegram.getFileLink(file.file_id);
            const savePath = path.join('/home/minhchau/Downloads', file.file_name);

            exec(`curl -o "${savePath}" "${link.href}"`, { timeout: 30000 }, (err) => {
                if (err) return ctx.reply(`Lỗi tải tệp: ${err.message}`);
                ctx.reply(`Đã lưu tệp vào: \`${savePath}\``, { parse_mode: 'Markdown' });
            });
        } catch (e) {
            ctx.reply(`Lỗi: ${e.message}`);
        }
    });
}

module.exports = { registerFileCommands };
