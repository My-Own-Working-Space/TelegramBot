/**
 * Cắt chuỗi theo độ dài tối đa, thêm "..." nếu bị cắt
 */
function truncate(str, maxLen = 4000) {
    if (!str) return '';
    if (str.length <= maxLen) return str;
    return str.slice(0, maxLen - 20) + '\n\n... (đã cắt bớt)';
}

module.exports = { truncate };
