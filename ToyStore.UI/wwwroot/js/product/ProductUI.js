const ProductUI = {
    // 1. Arayüzdeki Rol Makyajı (SIFIR HARDCODED)
    applyRoleBasedUI: (role) => {
        const layoutTitle = document.querySelector('header h2');
        const pageDesc = document.getElementById('pageDescription');
        const btnAdd = document.getElementById('btnAddNewProduct');
        const thActions = document.getElementById('actionsColumnHeader');

        if (role !== window.AppRoles.ADMIN) {
            if (layoutTitle) layoutTitle.textContent = "Oyuncak Kataloğu";
            if (pageDesc) pageDesc.textContent = "Sistemde kayıtlı olan tüm oyuncakları buradan inceleyebilirsiniz.";
            if (btnAdd) btnAdd.classList.add('hidden');
            if (thActions) thActions.classList.add('hidden');
            document.title = "Oyuncak Kataloğu - ToyStore";
        } else {
            if (layoutTitle) layoutTitle.textContent = "Oyuncak Yönetimi";
            if (pageDesc) pageDesc.textContent = "Sistemde kayıtlı olan tüm oyuncakların envanter kayıtlarını, fiyatlandırma politikalarını ve stok durumlarını buradan yönetebilirsiniz.";
            if (btnAdd) btnAdd.classList.remove('hidden');
            if (thActions) thActions.classList.remove('hidden');
        }
    },

    // 2. Kategori Dropdown'unu Doldurma
    populateCategoryDropdown: (categories) => {
        const selectElement = document.getElementById("productCategory");
        if (!selectElement) return;

        selectElement.innerHTML = '<option value="">Seçiniz...</option>';
        categories.forEach(category => {
            const option = document.createElement("option");
            option.value = category.id;
            option.textContent = category.name;
            selectElement.appendChild(option);
        });
    },

    // 3. Tabloyu Çizme (XSS Korumalı)
    renderTable: (paginatedProducts, currentUserRole) => {
        const tbody = document.getElementById("productsTableBody");
        if (!tbody) return;

        tbody.innerHTML = "";
        const isAdmin = currentUserRole === window.AppRoles.ADMIN;

        if (paginatedProducts.length === 0) {
            const colSpan = isAdmin ? "8" : "7";
            const tr = document.createElement("tr");
            tr.innerHTML = `<td colspan="${colSpan}" class="px-6 py-4 text-center text-sm text-gray-500">Kayıt bulunamadı.</td>`;
            tbody.appendChild(tr);
            return;
        }

        paginatedProducts.forEach(product => {
            const tr = document.createElement("tr");
            tr.className = "hover:bg-gray-50 transition-colors";

            const createCell = (text, className = "") => {
                const td = document.createElement("td");
                td.className = "px-6 py-4 whitespace-nowrap text-sm " + className;
                td.textContent = text;
                return td;
            };

            tr.appendChild(createCell(product.id || '-', 'text-gray-900'));
            tr.appendChild(createCell(product.name || '-', 'text-gray-900 font-medium'));
            tr.appendChild(createCell(product.categoryName || '-', 'text-gray-500'));

            const formattedPrice = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(product.price || 0);
            tr.appendChild(createCell(formattedPrice, 'text-gray-900'));
            tr.appendChild(createCell(product.stock || 0, 'text-gray-900'));

            const statusTd = document.createElement("td");
            statusTd.className = "px-6 py-4 whitespace-nowrap";
            const isProductActive = product.isActive === true || product.IsActive === true;
            const badge = document.createElement("span");
            badge.className = `px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${isProductActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`;
            badge.textContent = isProductActive ? 'Aktif' : 'Pasif';
            statusTd.appendChild(badge);
            tr.appendChild(statusTd);

            const favTd = document.createElement("td");
            favTd.className = "px-6 py-4 whitespace-nowrap text-center text-sm font-medium";
            const favBtn = document.createElement("button");
            favBtn.className = "text-2xl focus:outline-none transition-transform hover:scale-110";
            favBtn.title = "Favorilere Ekle/Çıkar";
            favBtn.textContent = (product.isFavorite || product.IsFavorite) ? "⭐" : "☆";
            favBtn.onclick = () => window.handleToggleFavorite(product.id);
            favTd.appendChild(favBtn);
            tr.appendChild(favTd);

            if (isAdmin) {
                const actionTd = document.createElement("td");
                actionTd.className = "px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-3";

                const editBtn = document.createElement("button");
                editBtn.textContent = "Düzenle";
                editBtn.className = "text-indigo-600 hover:text-indigo-900 transition-colors";
                editBtn.onclick = () => window.handleEditProduct(product);
                actionTd.appendChild(editBtn);

                const toggleBtn = document.createElement("button");
                toggleBtn.textContent = isProductActive ? 'Sil (Pasif Et)' : 'Aktif Et';
                toggleBtn.className = `text-${isProductActive ? 'red' : 'green'}-600 hover:text-${isProductActive ? 'red' : 'green'}-900 transition-colors`;
                toggleBtn.onclick = () => window.handleToggleStatus(product.id, isProductActive);
                actionTd.appendChild(toggleBtn);

                tr.appendChild(actionTd);
            }

            tbody.appendChild(tr);
        });
    },

    // 4. Sıralama Oklarını Güncelleme
    updateSortIcons: (currentSortColumn, isAscending) => {
        const columns = ['id', 'name', 'categoryName', 'price', 'stock', 'isActive', 'isFavorite'];
        columns.forEach(col => {
            const iconElement = document.getElementById(`sort-icon-${col}`);
            if (iconElement) {
                if (currentSortColumn === col) {
                    iconElement.innerHTML = isAscending ? "▲" : "▼";
                    iconElement.classList.remove("text-gray-400");
                    iconElement.classList.add("text-blue-600");
                } else {
                    iconElement.innerHTML = "↕";
                    iconElement.classList.remove("text-blue-600");
                    iconElement.classList.add("text-gray-400");
                }
            }
        });
    },

    // 5. Modal Yönetimi
    openModal: (product = null) => {
        const isEdit = product !== null;
        document.getElementById("productModalLabel").textContent = isEdit ? "Ürünü Düzenle" : "Yeni Ürün Ekle";

        document.getElementById("productId").value = isEdit ? product.id : "0";
        document.getElementById("productRowVersion").value = isEdit ? (product.rowVersion || "") : "";
        document.getElementById("productName").value = isEdit ? product.name : "";
        document.getElementById("productPrice").value = isEdit ? product.price : "";
        document.getElementById("productStock").value = isEdit ? product.stock : "";
        document.getElementById("productCategory").value = isEdit ? product.categoryId : "";

        document.getElementById("productModal").classList.remove("hidden");
    },

    closeModal: () => {
        document.getElementById("productModal").classList.add("hidden");
        document.getElementById("productForm").reset();
    }
};