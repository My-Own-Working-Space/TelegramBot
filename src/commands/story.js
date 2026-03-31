const { exec } = require('child_process');
const fs = require('fs');
const path = require('path');
const { truncate } = require('../utils');

const STATE_FILE = path.join(__dirname, '../../user_data/story_state.json');
const MAX_MSG_LEN = 4000;

function saveState(state) {
    fs.writeFileSync(STATE_FILE, JSON.stringify(state, null, 2));
}

function loadState() {
    if (fs.existsSync(STATE_FILE)) {
        return JSON.parse(fs.readFileSync(STATE_FILE, 'utf-8'));
    }
    return null;
}

function registerStoryCommands(bot) {
    bot.command('story', async (ctx) => {
        const topic = ctx.message.text.replace('/story', '').trim();
        if (!topic) return ctx.reply('Cú pháp: /story <chủ đề>\nVí dụ: /story Một chuyến phiêu lưu trong rừng rậm');

        await ctx.reply(`Đang bắt đầu câu chuyện về: ${topic}...`);
        ctx.sendChatAction('typing');

        const prompt = `Viết chương 1 cho một câu chuyện về chủ đề: "${topic}". Hãy làm cho nó kịch tính và kết thúc bằng một tình huống mở. Trả về tiếng Việt có dấu.`;
        const escaped = prompt.replace(/'/g, "'\\''");

        exec(`echo '${escaped}' | gemini`, { timeout: 120000 }, (err, stdout, stderr) => {
            if (err) return ctx.reply(`Lỗi: ${err.message}`);
            const chapter = stdout || stderr || 'Không có nội dung';
            const state = {
                topic: topic,
                history: [chapter],
                lastChapterTime: Date.now()
            };
            saveState(state);
            ctx.reply(`*Chương 1*\n\n${truncate(chapter, MAX_MSG_LEN - 50)}`, { parse_mode: 'Markdown' });
            ctx.reply('Bạn có thể nhắn "chương tiếp" hoặc "chi tiết chương tiếp" để tiếp tục câu chuyện.');
        });
    });

    bot.on('text', async (ctx, next) => {
        const text = ctx.message.text.toLowerCase().trim();
        if (text !== 'chương tiếp' && text !== 'chi tiết chương tiếp' && text !== 'chuong tiep' && text !== 'chi tiet chuong tiep') return next();

        const state = loadState();
        if (!state) return ctx.reply('Chưa có câu chuyện nào đang viết. Dùng /story để bắt đầu.');

        await ctx.reply(`Đang viết chương ${state.history.length + 1}...`);
        ctx.sendChatAction('typing');

        const lastChapters = state.history.slice(-2).join('\n\n');
        const prompt = `Đây là nội dung các chương trước:\n${lastChapters}\n\nHãy viết tiếp chương ${state.history.length + 1} cho câu chuyện này. Trả về tiếng Việt có dấu. ${text.includes('chi tiết') ? 'Hãy viết thật chi tiết và kịch tính.' : 'Hãy viết kịch tính và kết thúc bằng một tình huống mở.'}`;
        const escaped = prompt.replace(/'/g, "'\\''");

        exec(`echo '${escaped}' | gemini`, { timeout: 120000 }, (err, stdout, stderr) => {
            if (err) return ctx.reply(`Lỗi: ${err.message}`);
            const chapter = stdout || stderr || 'Không có nội dung';
            state.history.push(chapter);
            saveState(state);
            ctx.reply(`*Chương ${state.history.length}*\n\n${truncate(chapter, MAX_MSG_LEN - 50)}`, { parse_mode: 'Markdown' });
        });
    });
}

module.exports = { registerStoryCommands };
