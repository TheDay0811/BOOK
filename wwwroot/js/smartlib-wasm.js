/**
 * SMARTLIB RUST WEBASSEMBLY ENGINE (Rust 1.100.0 -> wasm32-unknown-unknown)
 * Tối ưu hóa tra cứu tức thì (0ms network latency), hỗ trợ Tiếng Việt không dấu & Fuzzy Search
 */
(function () {
    const encoder = new TextEncoder();
    const decoder = new TextDecoder('utf-8');

    let wasmExports = null;
    let catalogCount = 0;
    let isReady = false;
    let initPromise = null;

    async function initRustWasm() {
        if (isReady) return true;
        if (initPromise) return initPromise;

        initPromise = (async () => {
            try {
                const t0 = performance.now();
                const [wasmResp, catalogResp] = await Promise.all([
                    fetch('/wasm/smartlib_wasm.wasm?v=20261126', { cache: 'no-cache' }),
                    fetch('/Book/WasmCatalog')
                ]);

                if (!wasmResp.ok || !catalogResp.ok) {
                    throw new Error('Không thể tải module Rust Wasm hoặc Catalog.');
                }

                const wasmBytes = await wasmResp.arrayBuffer();
                const wasmModule = await WebAssembly.instantiate(wasmBytes, {});
                wasmExports = wasmModule.instance.exports;

                const tsvData = await catalogResp.text();
                const encoded = encoder.encode(tsvData);
                const ptr = wasmExports.wasm_alloc(encoded.length);
                new Uint8Array(wasmExports.memory.buffer, ptr, encoded.length).set(encoded);

                catalogCount = wasmExports.wasm_index_catalog(ptr, encoded.length);
                wasmExports.wasm_dealloc(ptr, encoded.length);

                isReady = true;
                const elapsed = (performance.now() - t0).toFixed(2);
                console.log(`[Rust Wasm 1.100] Đã nạp ${catalogCount} đầu sách vào bộ nhớ Wasm trong ${elapsed}ms.`);

                // Cập nhật badge trạng thái Rust Wasm trên giao diện nếu có
                document.querySelectorAll('[data-rust-wasm-badge]').forEach(el => {
                    el.classList.remove('hidden');
                    const textEl = el.querySelector('[data-rust-wasm-status]');
                    if (textEl) {
                        textEl.textContent = `Rust Wasm Ready (${catalogCount} sách • ${elapsed}ms)`;
                    }
                });

                return true;
            } catch (err) {
                console.warn('[Rust Wasm] Lỗi khởi tạo:', err);
                return false;
            }
        })();

        return initPromise;
    }

    function searchWithRust(query, onlyAvailable = false, limit = 12) {
        if (!isReady || !wasmExports) return { results: [], elapsedMicros: 0 };

        const t0 = performance.now();
        const encoded = encoder.encode(query || '');
        const ptr = wasmExports.wasm_alloc(encoded.length || 1);
        if (encoded.length > 0) {
            new Uint8Array(wasmExports.memory.buffer, ptr, encoded.length).set(encoded);
        }

        const outLen = wasmExports.wasm_search_books(
            ptr,
            encoded.length,
            onlyAvailable ? 1 : 0,
            limit
        );
        wasmExports.wasm_dealloc(ptr, encoded.length || 1);

        const outPtr = wasmExports.wasm_output_ptr();
        const outBytes = new Uint8Array(wasmExports.memory.buffer, outPtr, outLen);
        const jsonStr = decoder.decode(outBytes);
        const elapsedMicros = Math.max(1, Math.round((performance.now() - t0) * 1000));

        try {
            return {
                results: JSON.parse(jsonStr),
                elapsedMicros
            };
        } catch {
            return { results: [], elapsedMicros };
        }
    }

    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str || '';
        return div.innerHTML;
    }

    // Gắn dropdown gợi ý kết quả tức thì cho các ô input tìm kiếm
    function attachInstantSearchDropdown(inputEl) {
        if (!inputEl || inputEl.dataset.rustBound === '1') return;
        inputEl.dataset.rustBound = '1';
        inputEl.setAttribute('autocomplete', 'off');

        const wrapper = inputEl.parentElement;
        if (!wrapper) return;
        if (getComputedStyle(wrapper).position === 'static') {
            wrapper.style.position = 'relative';
        }

        const dropdown = document.createElement('div');
        dropdown.className = 'hidden absolute right-0 top-full mt-3 w-[26rem] max-w-[92vw] bg-white rounded-2xl shadow-2xl ring-1 ring-slate-900/10 z-50 overflow-hidden text-left';
        wrapper.appendChild(dropdown);

        // Rust Wasm render toàn bộ HTML danh sách gợi ý (đã escape an toàn)
        const renderWithRust = (q, limit) => {
            const enc = encoder.encode(q);
            const ptr = wasmExports.wasm_alloc(enc.length || 1);
            new Uint8Array(wasmExports.memory.buffer, ptr, enc.length).set(enc);
            const t0 = performance.now();
            const len = wasmExports.wasm_render_suggestions(ptr, enc.length, limit);
            const micros = Math.max(1, Math.round((performance.now() - t0) * 1000));
            wasmExports.wasm_dealloc(ptr, enc.length || 1);
            const html = decoder.decode(new Uint8Array(wasmExports.memory.buffer, wasmExports.wasm_output_ptr(), len));
            return { html, micros };
        };

        const renderSuggestions = async () => {
            const q = inputEl.value.trim();
            if (!q) {
                dropdown.classList.add('hidden');
                return;
            }

            await initRustWasm();
            if (!isReady) return;

            const { html, micros } = renderWithRust(q, 6);

            dropdown.innerHTML = `
                <div class="px-4 py-2.5 bg-gradient-to-r from-navy-950 to-navy-800 text-white flex items-center justify-between gap-3 text-[11px]">
                    <span class="font-semibold flex items-center gap-2 whitespace-nowrap">
                        <span class="inline-block w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
                        Rust Wasm Search
                        <span class="hidden sm:inline text-blue-200 font-normal">· không dấu &amp; sai chính tả</span>
                    </span>
                    <span class="font-mono text-amber-300 font-bold whitespace-nowrap">${micros} µs</span>
                </div>
                <div class="max-h-96 overflow-y-auto">${html}</div>
                <a href="/Book?searchQuery=${encodeURIComponent(q)}" class="block px-4 py-2.5 text-center text-xs font-bold text-navy-800 bg-slate-50 hover:bg-slate-100 border-t border-slate-100 transition-colors">Xem tất cả kết quả cho "${escapeHtml(q)}" →</a>
            `;
            dropdown.classList.remove('hidden');
        };

        inputEl.addEventListener('input', renderSuggestions);
        inputEl.addEventListener('focus', () => {
            if (inputEl.value.trim().length > 0) renderSuggestions();
        });

        document.addEventListener('click', (e) => {
            if (!wrapper.contains(e.target)) {
                dropdown.classList.add('hidden');
            }
        });
    }

    // Lọc trực tiếp các thẻ sách trên trang Danh mục sách (/Book/Index) bằng Rust Wasm
    function attachCatalogLiveFilter() {
        const filterInput = document.querySelector('#filterForm input[name="SearchQuery"]');
        const bookCards = document.querySelectorAll('[data-book-card-id]');
        if (!filterInput || bookCards.length === 0) return;

        const metricBadge = document.getElementById('rustWasmLiveMetric');

        filterInput.addEventListener('input', async () => {
            const q = filterInput.value.trim();
            await initRustWasm();
            if (!isReady) return;

            if (!q) {
                bookCards.forEach(card => card.classList.remove('hidden'));
                if (metricBadge) {
                    metricBadge.textContent = `Rust Wasm Ready (${catalogCount} sách)`;
                }
                return;
            }

            const { results, elapsedMicros } = searchWithRust(q, false, 100);
            const matchedIds = new Set(results.map(r => String(r.id)));

            let visibleCount = 0;
            bookCards.forEach(card => {
                const id = card.getAttribute('data-book-card-id');
                if (matchedIds.has(id)) {
                    card.classList.remove('hidden');
                    visibleCount++;
                } else {
                    card.classList.add('hidden');
                }
            });

            if (metricBadge) {
                metricBadge.textContent = `Rust Wasm: ${visibleCount} kết quả trong ${elapsedMicros} µs`;
            }
        });
    }

    window.SmartLibWasm = {
        init: initRustWasm,
        search: searchWithRust
    };

    document.addEventListener('DOMContentLoaded', () => {
        // Khởi tạo Wasm ngay khi trang tải xong
        initRustWasm();

        // Gắn Instant Search cho thanh tìm kiếm trên Header và Trang chủ
        document.querySelectorAll('input[name="searchQuery"]').forEach(attachInstantSearchDropdown);

        // Gắn bộ lọc trực tiếp trên trang /Book/Index
        attachCatalogLiveFilter();
    });
})();
