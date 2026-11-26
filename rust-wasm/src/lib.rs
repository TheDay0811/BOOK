use std::cell::RefCell;
use std::slice;
use std::str;

#[derive(Clone)]
struct BookRecord {
    id: u32,
    title: String,
    author: String,
    category: String,
    isbn: String,
    year: u32,
    available: u32,
    total: u32,
    cover: String,
    shelf: String,
    norm_title: String,
    norm_author: String,
    norm_category: String,
    norm_isbn: String,
}

thread_local! {
    static CATALOG: RefCell<Vec<BookRecord>> = RefCell::new(Vec::new());
    static OUTPUT_BUF: RefCell<Vec<u8>> = RefCell::new(Vec::new());
}

/// Chuyển đổi chuỗi Tiếng Việt có dấu sang không dấu + chữ thường
fn normalize_vi(input: &str) -> String {
    let mut out = String::with_capacity(input.len());
    for c in input.chars() {
        let mapped = match c {
            'à' | 'á' | 'ạ' | 'ả' | 'ã' | 'â' | 'ầ' | 'ấ' | 'ậ' | 'ẩ' | 'ẫ' | 'ă' | 'ằ' | 'ắ' | 'ặ' | 'ẳ' | 'ẵ' |
            'À' | 'Á' | 'Ạ' | 'Ả' | 'Ã' | 'Â' | 'Ầ' | 'Ấ' | 'Ậ' | 'Ẩ' | 'Ẫ' | 'Ă' | 'Ằ' | 'Ắ' | 'Ặ' | 'Ẳ' | 'Ẵ' => 'a',
            'è' | 'é' | 'ẹ' | 'ẻ' | 'ẽ' | 'ê' | 'ề' | 'ế' | 'ệ' | 'ể' | 'ễ' |
            'È' | 'É' | 'Ẹ' | 'Ẻ' | 'Ẽ' | 'Ê' | 'Ề' | 'Ế' | 'Ệ' | 'Ể' | 'Ễ' => 'e',
            'ì' | 'í' | 'ị' | 'ỉ' | 'ĩ' |
            'Ì' | 'Í' | 'Ị' | 'Ỉ' | 'Ĩ' => 'i',
            'ò' | 'ó' | 'ọ' | 'ỏ' | 'õ' | 'ô' | 'ồ' | 'ố' | 'ộ' | 'ổ' | 'ỗ' | 'ơ' | 'ờ' | 'ớ' | 'ợ' | 'ở' | 'ỡ' |
            'Ò' | 'Ó' | 'Ọ' | 'Ỏ' | 'Õ' | 'Ô' | 'Ồ' | 'Ố' | 'Ộ' | 'Ổ' | 'Ỗ' | 'Ơ' | 'Ờ' | 'Ớ' | 'Ợ' | 'Ở' | 'Ỡ' => 'o',
            'ù' | 'ú' | 'ụ' | 'ủ' | 'ũ' | 'ư' | 'ừ' | 'ứ' | 'ự' | 'ử' | 'ữ' |
            'Ù' | 'Ú' | 'Ụ' | 'Ủ' | 'Ũ' | 'Ư' | 'Ừ' | 'Ứ' | 'Ự' | 'Ử' | 'Ữ' => 'u',
            'ỳ' | 'ý' | 'ỵ' | 'ỷ' | 'ỹ' |
            'Ỳ' | 'Ý' | 'Ỵ' | 'Ỷ' | 'Ỹ' => 'y',
            'đ' | 'Đ' => 'd',
            _ => c.to_ascii_lowercase(),
        };
        out.push(mapped);
    }
    out
}

/// Tính khoảng cách Levenshtein (Bounded) giữa 2 từ để bắt lỗi gõ sai chính tả
fn edit_distance_bounded(a: &str, b: &str, max_dist: usize) -> usize {
    let a_chars: Vec<char> = a.chars().collect();
    let b_chars: Vec<char> = b.chars().collect();
    let m = a_chars.len();
    let n = b_chars.len();

    if m.abs_diff(n) > max_dist {
        return max_dist + 1;
    }
    if m == 0 {
        return n;
    }
    if n == 0 {
        return m;
    }

    let mut prev: Vec<usize> = (0..=n).collect();
    let mut curr: Vec<usize> = vec![0; n + 1];

    for i in 1..=m {
        curr[0] = i;
        let mut min_row = curr[0];
        for j in 1..=n {
            let cost = if a_chars[i - 1] == b_chars[j - 1] { 0 } else { 1 };
            curr[j] = (prev[j] + 1)
                .min(curr[j - 1] + 1)
                .min(prev[j - 1] + cost);
            if curr[j] < min_row {
                min_row = curr[j];
            }
        }
        if min_row > max_dist {
            return max_dist + 1;
        }
        std::mem::swap(&mut prev, &mut curr);
    }
    prev[n]
}

/// Chấm điểm mức độ khớp của một cuốn sách với từ khóa tìm kiếm
fn score_book(book: &BookRecord, norm_query: &str, tokens: &[&str]) -> u32 {
    if norm_query.is_empty() {
        return 1;
    }

    let mut total_score: u32 = 0;

    // 1. Khớp chính xác cụm từ
    if book.norm_isbn.contains(norm_query) {
        total_score += 200;
    }
    if book.norm_title == norm_query {
        total_score += 250;
    } else if book.norm_title.starts_with(norm_query) {
        total_score += 160;
    } else if book.norm_title.contains(norm_query) {
        total_score += 120;
    }

    if book.norm_author.contains(norm_query) {
        total_score += 90;
    }
    if book.norm_category.contains(norm_query) {
        total_score += 65;
    }

    // 2. Chấm điểm theo từng từ (token) + Fuzzy matching (chấp nhận sai chính tả)
    let title_words: Vec<&str> = book.norm_title.split_whitespace().collect();
    let author_words: Vec<&str> = book.norm_author.split_whitespace().collect();

    let mut matched_tokens = 0usize;

    for &tk in tokens {
        if tk.is_empty() {
            continue;
        }
        let mut token_score = 0u32;

        if book.norm_title.contains(tk) {
            token_score += 40;
            if title_words.iter().any(|&w| w == tk) {
                token_score += 25;
            } else if title_words.iter().any(|&w| w.starts_with(tk)) {
                token_score += 15;
            }
        } else if tk.len() >= 4 {
            // Kiểm tra sai chính tả tối đa 1 ký tự trên từng từ tiêu đề
            for &w in &title_words {
                if w.len() >= 4 && edit_distance_bounded(tk, w, 1) <= 1 {
                    token_score += 22;
                    break;
                }
            }
        }

        if book.norm_author.contains(tk) {
            token_score += 30;
        } else if tk.len() >= 4 {
            for &w in &author_words {
                if w.len() >= 4 && edit_distance_bounded(tk, w, 1) <= 1 {
                    token_score += 15;
                    break;
                }
            }
        }

        if book.norm_category.contains(tk) {
            token_score += 20;
        }
        if book.norm_isbn.contains(tk) {
            token_score += 50;
        }

        if token_score > 0 {
            matched_tokens += 1;
            total_score += token_score;
        }
    }

    // Nếu người dùng gõ nhiều từ nhưng không khớp đủ ít nhất một nửa số từ thì loại trừ
    if tokens.len() > 1 && matched_tokens * 2 < tokens.len() && total_score < 90 {
        return 0;
    }

    // Cộng điểm ưu tiên nhẹ cho sách còn bản sao trong kho
    if total_score > 0 && book.available > 0 {
        total_score += 5;
    }

    total_score
}

fn escape_json(s: &str, out: &mut String) {
    for c in s.chars() {
        match c {
            '"' => out.push_str("\\\""),
            '\\' => out.push_str("\\\\"),
            '\n' => out.push_str("\\n"),
            '\r' => out.push_str("\\r"),
            '\t' => out.push_str("\\t"),
            c if (c as u32) < 0x20 => {}
            c => out.push(c),
        }
    }
}

#[no_mangle]
pub extern "C" fn wasm_alloc(size: usize) -> *mut u8 {
    let mut buf = Vec::with_capacity(size);
    let ptr = buf.as_mut_ptr();
    std::mem::forget(buf);
    ptr
}

#[no_mangle]
pub extern "C" fn wasm_dealloc(ptr: *mut u8, size: usize) {
    unsafe {
        let _ = Vec::from_raw_parts(ptr, 0, size);
    }
}

#[no_mangle]
pub extern "C" fn wasm_output_ptr() -> *const u8 {
    OUTPUT_BUF.with(|buf| buf.borrow().as_ptr())
}

#[no_mangle]
pub extern "C" fn wasm_output_len() -> usize {
    OUTPUT_BUF.with(|buf| buf.borrow().len())
}

/// Nạp danh mục sách định dạng TSV (mỗi dòng 1 cuốn, các trường ngăn cách bởi dấu Tab `\t`)
/// Định dạng dòng: id \t title \t author \t category \t isbn \t year \t available \t total \t cover \t shelf
#[no_mangle]
pub extern "C" fn wasm_index_catalog(ptr: *const u8, len: usize) -> usize {
    let input = unsafe {
        let bytes = slice::from_raw_parts(ptr, len);
        str::from_utf8_unchecked(bytes)
    };

    let mut records = Vec::new();
    for line in input.lines() {
        if line.trim().is_empty() {
            continue;
        }
        let parts: Vec<&str> = line.split('\t').collect();
        if parts.len() < 10 {
            continue;
        }
        let id = parts[0].parse::<u32>().unwrap_or(0);
        let title = parts[1].to_string();
        let author = parts[2].to_string();
        let category = parts[3].to_string();
        let isbn = parts[4].to_string();
        let year = parts[5].parse::<u32>().unwrap_or(2024);
        let available = parts[6].parse::<u32>().unwrap_or(0);
        let total = parts[7].parse::<u32>().unwrap_or(0);
        let cover = parts[8].to_string();
        let shelf = parts[9].to_string();

        let norm_title = normalize_vi(&title);
        let norm_author = normalize_vi(&author);
        let norm_category = normalize_vi(&category);
        let norm_isbn = normalize_vi(&isbn);

        records.push(BookRecord {
            id,
            title,
            author,
            category,
            isbn,
            year,
            available,
            total,
            cover,
            shelf,
            norm_title,
            norm_author,
            norm_category,
            norm_isbn,
        });
    }

    let count = records.len();
    CATALOG.with(|cat| {
        *cat.borrow_mut() = records;
    });
    count
}

fn escape_html(s: &str, out: &mut String) {
    for c in s.chars() {
        match c {
            '&' => out.push_str("&amp;"),
            '<' => out.push_str("&lt;"),
            '>' => out.push_str("&gt;"),
            '"' => out.push_str("&quot;"),
            '\'' => out.push_str("&#39;"),
            c => out.push(c),
        }
    }
}

/// Render toàn bộ phần thân dropdown gợi ý tìm kiếm trên thanh header bằng Rust (HTML an toàn, đã escape)
#[no_mangle]
pub extern "C" fn wasm_render_suggestions(q_ptr: *const u8, q_len: usize, limit: usize) -> usize {
    let query = unsafe {
        let bytes = slice::from_raw_parts(q_ptr, q_len);
        str::from_utf8_unchecked(bytes)
    };
    let raw_q = query.trim();
    let norm_q = normalize_vi(raw_q);
    let tokens: Vec<&str> = norm_q.split_whitespace().collect();

    let mut html = String::with_capacity(2048);

    CATALOG.with(|cat| {
        let catalog = cat.borrow();
        let mut scored: Vec<(u32, usize)> = Vec::new();
        for (idx, book) in catalog.iter().enumerate() {
            let sc = score_book(book, &norm_q, &tokens);
            if sc > 0 {
                scored.push((sc, idx));
            }
        }
        scored.sort_unstable_by(|a, b| b.0.cmp(&a.0));
        let take = scored.len().min(if limit == 0 { 6 } else { limit });

        if take == 0 {
            html.push_str("<div class=\"px-5 py-8 text-center\"><div class=\"mx-auto mb-2 w-10 h-10 rounded-full bg-slate-100 flex items-center justify-center text-slate-400 text-lg\">&#128269;</div><p class=\"text-sm text-slate-500\">Không tìm thấy sách khớp với <strong class=\"text-slate-800\">");
            escape_html(raw_q, &mut html);
            html.push_str("</strong></p><p class=\"text-xs text-slate-400 mt-1\">Thử gõ không dấu hoặc rút gọn từ khóa</p></div>");
            return;
        }

        html.push_str("<ul class=\"divide-y divide-slate-100\">");
        for &(_, idx) in scored.iter().take(take) {
            let b = &catalog[idx];
            html.push_str("<li><a href=\"/Book/Details/");
            html.push_str(&b.id.to_string());
            html.push_str("\" class=\"flex items-center gap-4 px-4 py-3 hover:bg-blue-50/70 transition-colors\"><img src=\"");
            escape_html(&b.cover, &mut html);
            html.push_str("\" alt=\"\" class=\"w-11 h-16 object-cover rounded-lg border border-slate-200 shadow-sm flex-shrink-0 bg-slate-100\" onerror=\"this.onerror=null;this.src='/uploads/books/default-book.svg';\"/><div class=\"flex-1 min-w-0\"><div class=\"flex items-center justify-between gap-2 mb-0.5\"><span class=\"text-[10px] font-bold uppercase tracking-wide text-blue-600 truncate\">");
            escape_html(&b.category, &mut html);
            html.push_str("</span>");
            if b.available > 0 {
                html.push_str("<span class=\"flex-shrink-0 px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200\">Còn ");
                html.push_str(&b.available.to_string());
                html.push('/');
                html.push_str(&b.total.to_string());
                html.push_str("</span>");
            } else {
                html.push_str("<span class=\"flex-shrink-0 px-2 py-0.5 rounded-full text-[10px] font-bold bg-rose-50 text-rose-700 border border-rose-200\">Hết sách</span>");
            }
            html.push_str("</div><h5 class=\"text-sm font-bold text-slate-900 truncate\">");
            escape_html(&b.title, &mut html);
            html.push_str("</h5><p class=\"text-xs text-slate-500 truncate\">");
            escape_html(&b.author, &mut html);
            html.push_str(" &bull; <span class=\"font-mono\">");
            escape_html(&b.isbn, &mut html);
            html.push_str("</span></p></div></a></li>");
        }
        html.push_str("</ul>");
    });

    OUTPUT_BUF.with(|out| {
        *out.borrow_mut() = html.into_bytes();
    });
    wasm_output_len()
}

/// Tìm kiếm sách siêu tốc bằng Rust Wasm, trả về độ dài chuỗi JSON trong OUTPUT_BUF
#[no_mangle]
pub extern "C" fn wasm_search_books(
    q_ptr: *const u8,
    q_len: usize,
    only_available: u32,
    limit: usize,
) -> usize {
    let query = unsafe {
        let bytes = slice::from_raw_parts(q_ptr, q_len);
        str::from_utf8_unchecked(bytes)
    };

    let norm_q = normalize_vi(query.trim());
    let tokens: Vec<&str> = norm_q.split_whitespace().collect();

    let mut scored: Vec<(u32, usize)> = Vec::new();

    CATALOG.with(|cat| {
        let catalog = cat.borrow();
        for (idx, book) in catalog.iter().enumerate() {
            if only_available == 1 && book.available == 0 {
                continue;
            }
            let sc = score_book(book, &norm_q, &tokens);
            if sc > 0 {
                scored.push((sc, idx));
            }
        }

        scored.sort_unstable_by(|a, b| b.0.cmp(&a.0));

        let take_count = scored.len().min(if limit == 0 { 50 } else { limit });
        let mut json = String::with_capacity(take_count * 220 + 32);
        json.push('[');

        for (i, &(score, idx)) in scored.iter().take(take_count).enumerate() {
            if i > 0 {
                json.push(',');
            }
            let b = &catalog[idx];
            json.push_str("{\"id\":");
            json.push_str(&b.id.to_string());
            json.push_str(",\"score\":");
            json.push_str(&score.to_string());
            json.push_str(",\"title\":\"");
            escape_json(&b.title, &mut json);
            json.push_str("\",\"author\":\"");
            escape_json(&b.author, &mut json);
            json.push_str("\",\"category\":\"");
            escape_json(&b.category, &mut json);
            json.push_str("\",\"isbn\":\"");
            escape_json(&b.isbn, &mut json);
            json.push_str("\",\"year\":");
            json.push_str(&b.year.to_string());
            json.push_str(",\"available\":");
            json.push_str(&b.available.to_string());
            json.push_str(",\"total\":");
            json.push_str(&b.total.to_string());
            json.push_str(",\"cover\":\"");
            escape_json(&b.cover, &mut json);
            json.push_str("\",\"shelf\":\"");
            escape_json(&b.shelf, &mut json);
            json.push_str("\"}");
        }
        json.push(']');

        OUTPUT_BUF.with(|out| {
            *out.borrow_mut() = json.into_bytes();
        });
    });

    wasm_output_len()
}
