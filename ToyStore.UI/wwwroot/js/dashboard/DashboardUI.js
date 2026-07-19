// XSS Koruması için verileri sanitize eden (temizleyen) kalkan fonksiyon
const escapeHTML = (val) => {
    if (val === null || val === undefined) return '';
    return String(val)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
};

const DashboardUI = {
    // 1. Dinamik İlerleme Çubuğu Yardımcısı
    createProgressBar: (activeCount, passiveCount) => {
        // Matematiksel işlemler için sayıya çevirme garantisi (Optimizasyon)
        const active = Number(activeCount) || 0;
        const passive = Number(passiveCount) || 0;
        const total = active + passive;

        if (total === 0) return `<div class="w-full bg-gray-200 h-2 rounded-full mt-2"></div><span class="text-xs text-gray-500 mt-1 block">%0 Aktif</span>`;

        const activePercent = Math.round((active / total) * 100);
        const passivePercent = 100 - activePercent;

        // Yüzdelik dilimler sayısal olsa bile template içine girerken escape edilir
        return `
            <div class="w-full bg-red-100 h-2 rounded-full mt-3 overflow-hidden flex" title="Aktif: %${escapeHTML(activePercent)} | Pasif: %${escapeHTML(passivePercent)}">
                <div class="bg-green-500 h-full transition-all duration-500" style="width: ${escapeHTML(activePercent)}%"></div>
            </div>
            <div class="flex justify-between items-center mt-1 text-xs font-medium text-gray-500">
                <span class="text-green-600">%${escapeHTML(activePercent)} Aktif</span>
                <span class="text-red-600">%${escapeHTML(passivePercent)} Pasif</span>
            </div>
        `;
    },

    // 2. Admin / Manager Görünümünü Render Etme (XSS Korumalı ve Yenileme Butonlu)
    renderAdminDashboard: (stats) => {
        const container = document.getElementById("dashboardContainer");
        if (!container) return;

        if (!stats) {
            container.innerHTML = `<div class="text-center text-gray-500 py-10">İstatistik verileri yüklenemedi.</div>`;
            return;
        }

        const totalProducts = stats.totalProducts ?? stats.TotalProducts ?? 0;
        const activeProducts = stats.activeProducts ?? stats.ActiveProducts ?? 0;
        const passiveProducts = stats.passiveProducts ?? stats.PassiveProducts ?? 0;

        const totalCategories = stats.totalCategories ?? stats.TotalCategories ?? 0;
        const activeCategories = stats.activeCategories ?? stats.ActiveCategories ?? 0;
        const passiveCategories = stats.passiveCategories ?? stats.PassiveCategories ?? 0;

        const totalUsers = stats.totalUsers ?? stats.TotalUsers ?? 0;
        const activeUsers = stats.activeUsers ?? stats.ActiveUsers ?? 0;
        const passiveUsers = stats.passiveUsers ?? stats.PassiveUsers ?? 0;

        const totalFavProducts = stats.totalFavoriteProducts ?? stats.TotalFavoriteProducts ?? stats.totalFavProducts ?? 0;
        const activeFavProducts = stats.activeFavoriteProducts ?? stats.ActiveFavoriteProducts ?? stats.activeFavProducts ?? 0;
        const passiveFavProducts = stats.passiveFavoriteProducts ?? stats.PassiveFavoriteProducts ?? stats.passiveFavProducts ?? 0;

        const totalFavCategories = stats.totalFavoriteCategories ?? stats.TotalFavoriteCategories ?? stats.totalFavCategories ?? 0;
        const activeFavCategories = stats.activeFavoriteCategories ?? stats.ActiveFavoriteCategories ?? stats.activeFavCategories ?? 0;
        const passiveFavCategories = stats.passiveFavoriteCategories ?? stats.PassiveFavoriteCategories ?? stats.passiveFavCategories ?? 0;

        const criticalProducts = stats.criticalStockProducts ?? stats.CriticalStockProducts ?? [];

        // ENTEGRASYON: Şablon tamamen temizlenip basılıyor, XSS kalkanı aktif.
        container.innerHTML = `
            <!-- Üst Başlık Grubu ve Canlı Yenileme Kısayolu -->
            <div class="flex justify-between items-center mb-6">
                <div>
                    <h1 class="text-2xl font-bold text-gray-900">Yönetim Paneli Özet İstatistikleri</h1>
                    <p class="text-sm text-gray-500 mt-1">Sistemdeki tüm canlı envanter, kullanıcı ve favori dağılımlarının anlık analizi.</p>
                </div>
                <button onclick="window.refreshDashboardData()" class="flex items-center gap-1.5 px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 text-sm font-medium rounded-lg shadow-sm transition-colors" title="Verileri Yenile">
                    🔄 Canlı Yenile
                </button>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-5 mb-8">
                <!-- KART 1: Toplam Ürün -->
                <div class="bg-white p-5 shadow rounded-lg border-l-4 border-blue-500 transition-transform hover:scale-105">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-xs font-semibold text-gray-400 uppercase tracking-wider">Toplam Ürün</p>
                            <h3 class="text-2xl font-bold text-gray-900 mt-1">${escapeHTML(totalProducts)}</h3>
                        </div>
                        <span class="text-2xl">📦</span>
                    </div>
                    <div class="mt-2 text-xs text-gray-600 font-medium">
                        <div>🟢 Aktif: <span class="font-bold text-green-600">${escapeHTML(activeProducts)}</span></div>
                        <div class="mt-0.5">🔴 Pasif: <span class="font-bold text-red-600">${escapeHTML(passiveProducts)}</span></div>
                    </div>
                    ${DashboardUI.createProgressBar(activeProducts, passiveProducts)}
                </div>

                <!-- KART 2: Toplam Kategori -->
                <div class="bg-white p-5 shadow rounded-lg border-l-4 border-indigo-500 transition-transform hover:scale-105">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-xs font-semibold text-gray-400 uppercase tracking-wider">Toplam Kategori</p>
                            <h3 class="text-2xl font-bold text-gray-900 mt-1">${escapeHTML(totalCategories)}</h3>
                        </div>
                        <span class="text-2xl">🗂️</span>
                    </div>
                    <div class="mt-2 text-xs text-gray-600 font-medium">
                        <div>🟢 Aktif: <span class="font-bold text-green-600">${escapeHTML(activeCategories)}</span></div>
                        <div class="mt-0.5">🔴 Pasif: <span class="font-bold text-red-600">${escapeHTML(passiveCategories)}</span></div>
                    </div>
                    ${DashboardUI.createProgressBar(activeCategories, passiveCategories)}
                </div>

                <!-- KART 3: Toplam Kullanıcı -->
                <div class="bg-white p-5 shadow rounded-lg border-l-4 border-purple-500 transition-transform hover:scale-105">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-xs font-semibold text-gray-400 uppercase tracking-wider">Toplam Kullanıcı</p>
                            <h3 class="text-2xl font-bold text-gray-900 mt-1">${escapeHTML(totalUsers)}</h3>
                        </div>
                        <span class="text-2xl">👥</span>
                    </div>
                    <div class="mt-2 text-xs text-gray-600 font-medium">
                        <div>🟢 Aktif: <span class="font-bold text-green-600">${escapeHTML(activeUsers)}</span></div>
                        <div class="mt-0.5">🔴 Pasif: <span class="font-bold text-red-600">${escapeHTML(passiveUsers)}</span></div>
                    </div>
                    ${DashboardUI.createProgressBar(activeUsers, passiveUsers)}
                </div>

                <!-- KART 4: Favori Ürünler -->
                <div class="bg-white p-5 shadow rounded-lg border-l-4 border-amber-500 transition-transform hover:scale-105">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-xs font-semibold text-gray-400 uppercase tracking-wider">Favori Ürünler</p>
                            <h3 class="text-2xl font-bold text-gray-900 mt-1">${escapeHTML(totalFavProducts)}</h3>
                        </div>
                        <span class="text-2xl">⭐</span>
                    </div>
                    <div class="mt-2 text-xs text-gray-600 font-medium">
                        <div>🟢 Aktif: <span class="font-bold text-green-600">${escapeHTML(activeFavProducts)}</span></div>
                        <div class="mt-0.5">🔴 Pasif: <span class="font-bold text-red-600">${escapeHTML(passiveFavProducts)}</span></div>
                    </div>
                    ${DashboardUI.createProgressBar(activeFavProducts, passiveFavProducts)}
                </div>

                <!-- KART 5: Favori Kategori -->
                <div class="bg-white p-5 shadow rounded-lg border-l-4 border-rose-500 transition-transform hover:scale-105">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-xs font-semibold text-gray-400 uppercase tracking-wider">Favori Kategori</p>
                            <h3 class="text-2xl font-bold text-gray-900 mt-1">${escapeHTML(totalFavCategories)}</h3>
                        </div>
                        <span class="text-2xl">❤️</span>
                    </div>
                    <div class="mt-2 text-xs text-gray-600 font-medium">
                        <div>🟢 Aktif: <span class="font-bold text-green-600">${escapeHTML(activeFavCategories)}</span></div>
                        <div class="mt-0.5">🔴 Pasif: <span class="font-bold text-red-600">${escapeHTML(passiveFavCategories)}</span></div>
                    </div>
                    ${DashboardUI.createProgressBar(activeFavCategories, passiveFavCategories)}
                </div>
            </div>

            <div class="bg-white shadow rounded-lg p-5 border border-red-200">
                <div class="flex items-center space-x-2 border-b border-gray-100 pb-3 mb-4">
                    <span class="text-xl">🚨</span>
                    <h3 class="text-lg font-bold text-red-700">Kritik Stok Seviyesindeki Ürünler (Limit <= 10)</h3>
                </div>
                <div class="w-full overflow-x-auto" id="criticalStockContainer"></div>
            </div>
        `;

        DashboardUI.renderCriticalStockTable(criticalProducts);
    },

    // 3. Kritik Stok Tablosu (XSS Korumalı & Hızlı Aksiyon Kısayollu)
    renderCriticalStockTable: (products) => {
        const container = document.getElementById("criticalStockContainer");
        if (!container) return;

        if (products.length === 0) {
            container.innerHTML = `<p class="text-sm text-green-600 font-medium py-2">🟢 Harika! Şu anda stok limiti kritik seviyede olan aktif ürün bulunmuyor.</p>`;
            return;
        }

        const table = document.createElement("table");
        table.className = "min-w-full divide-y divide-gray-200 text-sm text-left";
        table.innerHTML = `
            <thead class="bg-red-50 text-red-800 font-semibold uppercase text-xs">
                <tr>
                    <th class="px-4 py-2">ID</th>
                    <th class="px-4 py-2">Ürün Adı</th>
                    <th class="px-4 py-2">Kategori</th>
                    <th class="px-4 py-2 text-center">Kalan Stok</th>
                    <th class="px-4 py-2 text-right">Aksiyon</th>
                </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 bg-white"></tbody>
        `;

        const tbody = table.querySelector("tbody");
        products.forEach(p => {
            const tr = document.createElement("tr");
            tr.className = "hover:bg-red-50/50 transition-colors";

            // XSS KALKANI: İçerikler kesinlikle innerHTML ile değil, textContent ile atanıyor
            const createTd = (text, className = "") => {
                const td = document.createElement("td");
                td.className = "px-4 py-2.5 whitespace-nowrap " + className;
                td.textContent = text;
                return td;
            };

            const pId = p.id ?? p.Id ?? "-";
            const pName = p.name ?? p.Name ?? "-";
            const pCat = p.categoryName ?? p.CategoryName ?? "-";
            const pStock = p.stock ?? p.Stock ?? 0;

            tr.appendChild(createTd(pId, "text-gray-500 font-mono"));
            tr.appendChild(createTd(pName, "text-gray-900 font-medium"));
            tr.appendChild(createTd(pCat, "text-gray-500"));
            tr.appendChild(createTd(pStock, "text-center font-bold text-red-600 bg-red-100/50 rounded"));

            // Hızlı Aksiyon Hücresi
            const actionTd = document.createElement("td");
            actionTd.className = "px-4 py-2.5 text-right whitespace-nowrap";

            const quickBtn = document.createElement("button");
            quickBtn.className = "px-3 py-1 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-semibold rounded shadow transition-colors";
            quickBtn.textContent = "Stok Güncelle";

            // Güvenli Tıklama Olayı (Parametreler string enjeksiyonuna kapalıdır)
            quickBtn.onclick = () => {
                if (typeof window.quickStockUpdate === "function") {
                    window.quickStockUpdate(pId, pName, pStock);
                }
            };

            actionTd.appendChild(quickBtn);
            tr.appendChild(actionTd);
            tbody.appendChild(tr);
        });

        container.appendChild(table);
    },

    // 4. Standart User Görünümünü Render Etme (XSS Korumalı)
    renderUserDashboard: (stats) => {
        const container = document.getElementById("dashboardContainer");
        if (!container) return;

        if (!stats) {
            container.innerHTML = `<div class="text-center text-gray-500 py-10">Kişisel verileriniz yüklenemedi.</div>`;
            return;
        }

        const totalFavProducts = stats.totalFavoriteProducts ?? stats.TotalFavoriteProducts ?? stats.totalFavProducts ?? 0;
        const activeFavProducts = stats.activeFavoriteProducts ?? stats.ActiveFavoriteProducts ?? stats.activeFavProducts ?? 0;
        const passiveFavProducts = stats.passiveFavoriteProducts ?? stats.PassiveFavoriteProducts ?? stats.passiveFavProducts ?? 0;

        const totalFavCategories = stats.totalFavoriteCategories ?? stats.TotalFavoriteCategories ?? stats.totalFavCategories ?? 0;
        const activeFavCategories = stats.activeFavoriteCategories ?? stats.ActiveFavoriteCategories ?? stats.activeFavCategories ?? 0;
        const passiveFavCategories = stats.passiveFavoriteCategories ?? stats.PassiveFavoriteCategories ?? stats.passiveFavCategories ?? 0;

        container.innerHTML = `
            <div class="bg-gradient-to-r from-blue-500 to-indigo-600 rounded-lg p-6 text-white shadow-md mb-8">
                <h1 class="text-2xl font-bold">ToyStore Oyuncak Dünyasına Hoş Geldiniz! 🎉</h1>
                <p class="text-blue-100 text-sm mt-1">Kataloğumuzda şu an aktif olarak sergilenen yüzlerce eğlenceli ve eğitici oyuncağı inceleyebilir, favorilerinize ekleyebilirsiniz.</p>
            </div>

            <div class="mb-4">
                <h2 class="text-xl font-bold text-gray-900">Kişisel Favori İstatistikleriniz</h2>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <!-- KART 1: Favori Ürünlerim -->
                <div class="bg-white p-6 shadow rounded-lg border-t-4 border-amber-500">
                    <div class="flex justify-between items-center mb-2">
                        <span class="text-sm font-semibold text-gray-400 uppercase">Favori Oyuncaklarım</span>
                        <span class="text-2xl">⭐</span>
                    </div>
                    <div class="text-3xl font-extrabold text-gray-900">${escapeHTML(totalFavProducts)}</div>
                    <div class="mt-3 text-xs font-medium text-gray-500">
                        <span class="text-green-600">🟢 ${escapeHTML(activeFavProducts)} Aktif Listede</span> • 
                        <span class="text-red-600">🔴 ${escapeHTML(passiveFavProducts)} Arşivlenen</span>
                    </div>
                    ${DashboardUI.createProgressBar(activeFavProducts, passiveFavProducts)}
                </div>

                <!-- KART 2: Favori Kategorilerim -->
                <div class="bg-white p-6 shadow rounded-lg border-t-4 border-rose-500">
                    <div class="flex justify-between items-center mb-2">
                        <span class="text-sm font-semibold text-gray-400 uppercase">Takip Ettiğim Kategoriler</span>
                        <span class="text-2xl">❤️</span>
                    </div>
                    <div class="text-3xl font-extrabold text-gray-900">${escapeHTML(totalFavCategories)}</div>
                    <div class="mt-3 text-xs font-medium text-gray-500">
                        <span class="text-green-600">🟢 ${escapeHTML(activeFavCategories)} Aktif Kategori</span> • 
                        <span class="text-red-600">🔴 ${escapeHTML(passiveFavCategories)} Pasif Kategori</span>
                    </div>
                    ${DashboardUI.createProgressBar(activeFavCategories, passiveFavCategories)}
                </div>
            </div>
        `;
    }
};