function authGuard(allowedChatId) {
    return (ctx, next) => {
        const chatId = String(ctx.chat?.id || ctx.from?.id);
        if (chatId !== allowedChatId) {
            console.log(`Truy cập trái phép từ Chat ID: ${chatId}`);
            return ctx.reply('Từ chối truy cập. Bạn không có quyền sử dụng bot này.');
        }
        return next();
    };
}

module.exports = { authGuard };
