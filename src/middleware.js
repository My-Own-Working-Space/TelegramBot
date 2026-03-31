function authGuard(allowedChatId) {
    return (ctx, next) => {
        const chatId = String(ctx.chat?.id || ctx.from?.id);
        if (chatId !== allowedChatId) {
            console.log(`Unauthorized access from Chat ID: ${chatId}`);
            return ctx.reply('Unauthorized. Ban khong co quyen su dung bot nay.');
        }
        return next();
    };
}

module.exports = { authGuard };
