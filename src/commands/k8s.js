const { exec } = require('child_process');
const { truncate } = require('../utils');

const K8S_DIR = '/home/minhchau/Documents/Documents/K8s/Setup';
const MAX_MSG_LEN = 4000;

function registerK8sCommands(bot) {
    bot.command('k8s', async (ctx) => {
        const cmd = ctx.message.text.replace('/k8s', '').trim();
        if (!cmd) return ctx.reply('Cú pháp: /k8s <lệnh kubectl>\nVí dụ: /k8s get pods');

        await ctx.reply(`Đang thực thi lệnh Kubernetes: \`${cmd}\`...`, { parse_mode: 'Markdown' });
        ctx.sendChatAction('typing');

        const fullCmd = `cd ${K8S_DIR} && kubectl ${cmd}`;

        exec(fullCmd, { timeout: 60000, maxBuffer: 1024 * 1024 }, (err, stdout, stderr) => {
            const output = stdout || stderr || (err ? err.message : 'Không có đầu ra');
            const result = truncate(output, MAX_MSG_LEN);
            ctx.reply(`\`\`\`\n${result}\n\`\`\``, { parse_mode: 'Markdown' }).catch(() => {
                ctx.reply(result);
            });
        });
    });
}

module.exports = { registerK8sCommands };
