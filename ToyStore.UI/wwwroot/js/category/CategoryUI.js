const CategoryUI = {
    // 1. Rol Bazlı UI Kısıtlamalarını Uygula
    applyRoleBasedUI: (role) => {
        const layoutTitle = document.querySelector('header h2');
        const pageDesc = document.getElementById('pageDescription');
        const btnAdd = document.getElementById('btnAddNewCategory');
        const thActions = document.getElementById('actionsColumnHeader');

        if (role !== window.AppRoles.ADMIN) {
            if (layoutTitle) layoutTitle.textContent = "Kategori Kataloğu";
            if (pageDesc) pageDesc.textContent = "Sistemde kayıtlı olan tüm kategorileri buradan inceleyebilirsiniz.";
            if (btnAdd) btnAdd.classList.add('hidden');
            if (thActions) thActions.classList.add('hidden');
            document.title = "Kategori Kataloğu - ToyStore";
        } else {
            if (layoutTitle) layoutTitle.textContent = "Kategori Yönetimi";
            if (pageDesc) pageDesc.textContent = "Sistemdeki tüm ürün kategorilerini, sınıflandırma yapılarını ve envanter dağılımlarını buradan yönetebilirsiniz.";
            if (btnAdd) btnAdd.classList.remove('hidden');
            if (thActions) thActions.classList.remove('hidden');
        }
    },

    // 2. Tabloyu Render Etme (XSS Korumalı)
    renderTable: (paginatedCategories, userRole) => {
        const tbody = document.getElementById("categoryTableBody");
        if (!tbody) return;

        tbody.innerHTML = "";
        const isAdmin = userRole === window.AppRoles.ADMIN;

        if (paginatedCategories.length === 0) {
            const colSpan = isAdmin ? "7" : "6";
            const tr = document.createElement("tr");
            tr.innerHTML = `<td colspan="${colSpan}" class="px-6 py-4 text-center text-sm text-gray-500">Kayıt bulunamadı.</td>`;
            tbody.appendChild(tr);
            return;
        }

        paginatedCategories.forEach(category => {
            const tr = document.createElement("tr");
            tr.className = "hover:bg-gray-50 transition-colors";

            const createCell = (text, className = "") => {
                const td = document.createElement("td");
                td.className = "px-6 py-4 whitespace-nowrap text-sm " + className;
                td.textContent = text;
                return td;
            };

            tr.appendChild(createCell(category.id, 'text-gray-900'));
            tr.appendChild(createCell(category.name, 'text-gray-900 font-medium'));

            const statusTd = document.createElement("td");
            statusTd.className = "px-6 py-4 whitespace-nowrap";
            const isCatActive = category.isActive === true || category.IsActive === true;
            const badge = document.createElement("span");
            badge.className = `px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${isCatActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`;
            badge.textContent = isCatActive ? 'Aktif' : 'Pasif';
            statusTd.appendChild(badge);
            tr.appendChild(statusTd);

            // 3. Ürün Adedi (Tooltip ve mantık revizyonu)
            const prodCountTd = createCell(category.productCount || 0, 'text-gray-900 cursor-help');
            // ESKİ: "aktif ve pasif olarak listelenen..."
            prodCountTd.setAttribute('title', "Bu kategori altında listelenen ve satışı devam eden aktif ürün (oyuncak) çeşidi adedi.");
            tr.appendChild(prodCountTd);

            // 4. Toplam Stok (Tooltip ve mantık revizyonu)
            const stockTd = createCell(category.totalStock || 0, 'text-gray-900 cursor-help');
            // ESKİ: "depodaki tüm oyuncakların..."
            stockTd.setAttribute('title', "Bu kategoriye ait aktif oyuncakların depodaki güncel ve fiziksel toplam stok adedi.");
            tr.appendChild(stockTd);

            const favTd = document.createElement("td");
            favTd.className = "px-6 py-4 whitespace-nowrap text-center text-sm font-medium";
            const favDiv = document.createElement("div");
            favDiv.className = "flex items-center justify-center gap-2";

            const favBtn = document.createElement("button");
            favBtn.className = "text-2xl focus:outline-none transition-transform hover:scale-110";
            favBtn.textContent = (category.isFavorite || category.IsFavorite) ? "⭐" : "☆";
            favBtn.onclick = () => window.handleToggleFavorite(category.id);

            const ratioSpan = document.createElement("span");
            ratioSpan.className = "text-xs font-semibold text-gray-600 bg-gray-100 px-2 py-1 rounded-full cursor-help";
            ratioSpan.textContent = `${category.favoritedProductCount ?? 0}/${category.productCount ?? 0}`;
            ratioSpan.setAttribute('title', 'Favori / Toplam Ürün Oranı');

            favDiv.appendChild(favBtn);
            favDiv.appendChild(ratioSpan);
            favTd.appendChild(favDiv);
            tr.appendChild(favTd);

            if (isAdmin) {
                const actionTd = document.createElement("td");
                actionTd.className = "px-6 py-4 whitespace-nowrap text-right text-sm font-medium";

                const editBtn = document.createElement("button");
                editBtn.textContent = "Düzenle";
                editBtn.className = "text-indigo-600 hover:text-indigo-900 font-medium transition-colors";
                editBtn.onclick = () => window.handleEditCategory(category);
                actionTd.appendChild(editBtn);

                const toggleBtn = document.createElement("button");
                toggleBtn.textContent = isCatActive ? 'Sil (Pasif Et)' : 'Aktif Et';
                toggleBtn.className = `text-${isCatActive ? 'red' : 'green'}-600 hover:text-${isCatActive ? 'red' : 'green'}-900 font-medium transition-colors ml-3`;
                toggleBtn.onclick = () => window.handleToggleStatus(category.id, isCatActive);
                actionTd.appendChild(toggleBtn);

                tr.appendChild(actionTd);
            }

            tbody.appendChild(tr);
        });
    },

    // 3. Sıralama İkonlarını Güncelleme
    updateSortIcons: (currentSort) => {
        ['id', 'name', 'isActive', 'productCount', 'totalStock', 'isFavorite'].forEach(col => {
            const iconElement = document.getElementById(`sort-icon-${col}`);
            if (iconElement) {
                // currentSort.column ile döngüdeki kolon adının eşleşme kontrolü
                if (currentSort.column === col) {
                    // desc false ise (yani Ascending/Artan) ▲, desc true ise (Descending/Azalan) ▼ basar
                    iconElement.innerHTML = !currentSort.desc ? "▲" : "▼";
                    iconElement.classList.remove("text-gray-400");
                    iconElement.classList.add("text-blue-600"); // Aktif sütun şık bir mavi olur
                } else {
                    // Aktif olmayan veya sıfırlanan tüm sütunlar nötr duruma bükülür
                    iconElement.innerHTML = "↕";
                    iconElement.classList.remove("text-blue-600");
                    iconElement.classList.add("text-gray-400");
                }
            }
        });
    },

    // 4. Modal İşlemleri
    openModal: (category = null) => {
        const isEdit = category !== null;
        document.getElementById('modalTitle').innerText = isEdit ? 'Kategoriyi Düzenle' : 'Yeni Kategori Ekle';
        document.getElementById('categoryName').value = isEdit ? category.name : '';
        document.getElementById('categoryId').value = isEdit ? category.id : '0';
        document.getElementById('categoryRowVersion').value = isEdit ? (category.rowVersion || '') : '';
        document.getElementById('categoryModal').classList.remove('hidden');
    },

    closeModal: () => {
        document.getElementById('categoryModal').classList.add('hidden');
    },

    // 5. Yükleniyor Durumu Yönetimi
    showLoading: (isLoading) => {
        const loader = document.getElementById('loadingState');
        const container = document.getElementById('tableContainer');
        if (loader && container) {
            if (isLoading) { loader.classList.remove('hidden'); container.classList.add('hidden'); }
            else { loader.classList.add('hidden'); container.classList.remove('hidden'); }
        }
    }
};