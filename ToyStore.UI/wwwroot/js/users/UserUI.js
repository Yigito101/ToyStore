const UserUI = {
    // 1. Tabloyu Render Etme Mantığı (XSS Korumalı metotlarla donatıldı)
    renderTable: (paginatedUsers, currentUserId) => {
        const tbody = document.getElementById("usersTableBody");
        if (!tbody) return;

        tbody.innerHTML = "";

        if (paginatedUsers.length === 0) {
            const tr = document.createElement("tr");
            tr.innerHTML = `<td colspan="5" class="px-6 py-4 text-center text-sm text-gray-500">Kayıt bulunamadı.</td>`;
            tbody.appendChild(tr);
            return;
        }

        paginatedUsers.forEach(user => {
            const isUserActive = user.isActive === true || user.IsActive === true;
            const isCurrentUser = (user.id == currentUserId);

            const tr = document.createElement("tr");
            tr.className = `${isCurrentUser ? "bg-blue-50 hover:bg-blue-100" : "bg-white hover:bg-gray-50"} transition-colors`;

            // Güvenli Hücre Oluşturucu (textContent ile XSS Koruması)
            const createCell = (text, className = "") => {
                const td = document.createElement("td");
                td.className = "px-6 py-4 whitespace-nowrap text-sm " + className;
                td.textContent = text;
                return td;
            };

            tr.appendChild(createCell(user.id, 'text-gray-900'));

            // E-posta Hücresi ve "Sen" Rozeti Entegrasyonu
            const emailTd = document.createElement("td");
            emailTd.className = "px-6 py-4 whitespace-nowrap text-sm text-gray-900 font-medium";
            emailTd.textContent = user.email + " ";

            if (isCurrentUser) {
                const youBadge = document.createElement("span");
                youBadge.className = "ml-2 px-2 py-0.5 inline-flex text-xs leading-4 font-medium rounded-full bg-blue-200 text-blue-800";
                youBadge.textContent = "Sen";
                emailTd.appendChild(youBadge);
            }
            tr.appendChild(emailTd);

            // Rol Hücresi (Merkezi Enum Süzgeci)
            const currentRole = Object.values(window.AppRoles).find(r => r === user.role) || window.AppRoles.USER;
            tr.appendChild(createCell(currentRole, 'text-gray-500'));

            // Durum Rozeti (Badge)
            const statusTd = document.createElement("td");
            statusTd.className = "px-6 py-4 whitespace-nowrap";
            const badge = document.createElement("span");
            badge.className = `px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${isUserActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`;
            badge.textContent = isUserActive ? 'Aktif' : 'Pasif';
            statusTd.appendChild(badge);
            tr.appendChild(statusTd);

            // İşlem Butonları
            const actionTd = document.createElement("td");
            actionTd.className = "px-6 py-4 whitespace-nowrap text-right text-sm font-medium";

            const editBtn = document.createElement("button");
            editBtn.textContent = "Düzenle";
            editBtn.className = "text-indigo-600 hover:text-indigo-900 transition-colors";
            editBtn.onclick = () => window.handleEditUser(user);
            actionTd.appendChild(editBtn);

            if (isCurrentUser) {
                const span = document.createElement("span");
                span.className = "text-gray-400 cursor-not-allowed ml-3 text-xs font-normal";
                span.textContent = "Pasif Edilemez";
                span.title = "Kendi hesabınızı pasif edemezsiniz";
                actionTd.appendChild(span);
            } else {
                const toggleBtn = document.createElement("button");
                toggleBtn.textContent = isUserActive ? "Pasif Et" : "Aktif Et";
                toggleBtn.className = `${isUserActive ? 'text-red-600 hover:text-red-900' : 'text-green-600 hover:text-green-900'} transition-colors ml-3`;
                toggleBtn.onclick = () => window.handleToggleStatus(user.id, isUserActive);
                actionTd.appendChild(toggleBtn);
            }

            tr.appendChild(actionTd);
            tbody.appendChild(tr);
        });
    },

    // 2. Sıralama Oklarını Güncelleme
    updateSortIcons: (currentSortColumn, isAscending) => {
        const columns = ['id', 'email', 'role', 'isActive'];
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

    // 3. Modal Yönetimi
    openModal: (user = null) => {
        const errorBox = document.getElementById("userModalError");
        if (errorBox) {
            errorBox.classList.add("hidden");
            errorBox.innerHTML = "";
        }

        const form = document.getElementById("userForm");
        if (form) form.reset();

        const emailInput = document.getElementById("userEmail");
        const passwordInput = document.getElementById("userPassword");
        const confirmPasswordInput = document.getElementById("confirmUserPassword");
        const userIdInput = document.getElementById("userId");
        const originalRoleInput = document.getElementById("originalUserRole");
        const roleInput = document.getElementById("userRole");
        const modalLabel = document.getElementById("userModalLabel");
        const passwordHint = document.getElementById("passwordHint");

        const isEdit = user !== null;

        if (isEdit) {
            if (modalLabel) modalLabel.textContent = "Kullanıcıyı Düzenle";
            if (userIdInput) userIdInput.value = user.id;
            if (originalRoleInput) originalRoleInput.value = user.role;

            if (emailInput) {
                emailInput.value = user.email;
                emailInput.readOnly = true;
                emailInput.classList.add("bg-gray-100");
            }

            if (roleInput) roleInput.value = user.role;
            if (passwordInput) passwordInput.required = false;
            if (confirmPasswordInput) confirmPasswordInput.required = false;
            if (passwordHint) passwordHint.textContent = "(Değiştirmek istemiyorsanız boş bırakın)";
        } else {
            if (modalLabel) modalLabel.textContent = "Yeni Kullanıcı Ekle";
            if (userIdInput) userIdInput.value = "0";
            if (originalRoleInput) originalRoleInput.value = "";

            if (emailInput) {
                emailInput.readOnly = false;
                emailInput.classList.remove("bg-gray-100");
            }

            if (passwordInput) passwordInput.required = true;
            if (confirmPasswordInput) confirmPasswordInput.required = true;
            if (passwordHint) passwordHint.textContent = "";
        }

        const modal = document.getElementById("userModal");
        if (modal) modal.classList.remove("hidden");
    },

    closeModal: () => {
        const modal = document.getElementById("userModal");
        if (modal) modal.classList.add("hidden");
    },

    // 4. Validasyon Hatalarını Modal İçinde Listeleme (XSS VE HİBRİT UYARI TEMİZLİĞİ)
    displayValidationErrors: (errorData, errorBoxIdOrElement) => {
        let errorBox = typeof errorBoxIdOrElement === 'string'
            ? document.getElementById(errorBoxIdOrElement)
            : errorBoxIdOrElement;

        if (!errorBox) {
            // Çirkin alert tamamen kaldırıldı, global toast tetikleniyor
            window.NotificationUI.show(errorData.Message || "Validasyon hatası oluştu.", "error");
            return;
        }

        errorBox.classList.remove('hidden');
        errorBox.innerHTML = "";

        // FluentValidation veya API ModelState hatalarını XSS korumalı listeleme
        if (errorData.errors) {
            const ul = document.createElement("ul");
            ul.className = "list-disc ml-5 text-sm";

            for (const field in errorData.errors) {
                errorData.errors[field].forEach(err => {
                    const li = document.createElement("li");
                    li.className = "text-red-600 font-medium";
                    li.textContent = `${field}: ${err}`; // Güvenli düz yazı ataması
                    ul.appendChild(li);
                });
            }
            errorBox.appendChild(ul);
        } else {
            const p = document.createElement("p");
            p.className = "text-red-600 text-sm font-medium";
            p.textContent = errorData.Message || "İşlem başarısız oldu.";
            errorBox.appendChild(p);
        }
    }
};