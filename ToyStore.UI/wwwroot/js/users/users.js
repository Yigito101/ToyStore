// --- 1. GLOBAL STATE ---
window.usersData = [];
window.currentSortColumn = '';
window.isAscending = true;
window.currentStatusFilter = window.AppStatuses.ALL;

// --- 2. YAŞAM DÖNGÜSÜ ---
document.addEventListener("DOMContentLoaded", async () => {
    const currentRole = window.getUserRole();

    // ROUTE GUARD GÜVENLİK KALKANI
    if (currentRole !== window.AppRoles.ADMIN) {
        window.location.replace("/Dashboard");
        return;
    }

    await fetchUsers();
});

// --- 3. VERİ YÖNETİMİ ---
window.fetchUsers = async function () {
    try {
        window.usersData = await UserApi.getAll();
        window.applyFiltersAndRender();
    } catch (error) {
        console.error("Kullanıcı yükleme hatası:", error);
    }
};

// --- 4. MERKEZİ FİLTRELEME, SIRALAMA VE SABİTLEME ---
window.applyFiltersAndRender = function () {
    const searchInput = document.getElementById("searchInput");
    const searchQuery = searchInput ? searchInput.value.toLowerCase().trim() : "";

    let currentUserId = null;
    // ÇÖZÜM: "Ben" kimliğimi doğru sekmeden tanımak için localStorage entegrasyonu
    const token = localStorage.getItem("token");
    if (token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            currentUserId = payload.nameid || payload.sub;
        } catch (e) {
            console.error("Token okunamadı:", e);
        }
    }

    // Filtreleme, Sıralama ve UI render işlemleri (Mevcut kodların aynen devam ediyor...)
    let filteredUsers = window.usersData.filter(user => {
        const isUserActive = user.isActive === true || user.IsActive === true;
        if (window.currentStatusFilter === window.AppStatuses.ACTIVE && !isUserActive) return false;
        if (window.currentStatusFilter === window.AppStatuses.INACTIVE && isUserActive) return false;
        return user.id.toString().includes(searchQuery) || user.email.toLowerCase().includes(searchQuery);
    });

    if (window.currentSortColumn !== '') {
        filteredUsers.sort((a, b) => {
            let valA = a[window.currentSortColumn];
            let valB = b[window.currentSortColumn];
            if (window.currentSortColumn === 'isActive') {
                valA = (a.isActive === true || a.IsActive === true) ? 1 : 0;
                valB = (b.isActive === true || b.IsActive === true) ? 1 : 0;
            } else if (typeof valA === 'string') {
                valA = valA.toLowerCase();
                valB = valB.toLowerCase();
            }
            if (valA < valB) return window.isAscending ? -1 : 1;
            if (valA > valB) return window.isAscending ? 1 : -1;
            return 0;
        });
    }

    if (currentUserId) {
        const currentUserIndex = filteredUsers.findIndex(u => u.id == currentUserId);
        if (currentUserIndex !== -1) {
            const currentUserObj = filteredUsers.splice(currentUserIndex, 1)[0];
            filteredUsers.unshift(currentUserObj);
        }
    }

    UserUI.updateSortIcons(window.currentSortColumn, window.isAscending);
    PaginationHelper.init(filteredUsers, (paginated) => {
        UserUI.renderTable(paginated, currentUserId);
    });
};

// --- 5. OLAY TETİKLEYİCİLERİ ---
window.filterByStatus = function (status) {
    window.currentStatusFilter = status;
    window.applyFiltersAndRender();
};

window.resetSorting = function () {
    window.currentSortColumn = null;
    window.isAscending = true;
    window.currentStatusFilter = window.AppStatuses.ALL;

    const searchInput = document.getElementById("searchInput");
    if (searchInput) searchInput.value = "";

    window.applyFiltersAndRender();
};

window.sortBy = function (columnName) {
    if (window.currentSortColumn === columnName) {
        window.isAscending = !window.isAscending;
    } else {
        window.currentSortColumn = columnName;
        window.isAscending = true;
    }
    window.applyFiltersAndRender();
};

window.searchUsers = function () {
    window.applyFiltersAndRender();
};

window.handleToggleFavorite = async (id) => {
    // Kullanıcı modülü favori desteklemiyor
};

// Aktif / Pasif Etme İşlemi (Hibrit Onay Temizlendi)
window.handleToggleStatus = async (id, isActiveNow) => {
    const actionText = isActiveNow ? "pasif etmek" : "tekrar aktif etmek";

    window.NotificationUI.confirm(`Bu kullanıcıyı ${actionText} istediğinize emin misiniz?`, async () => {
        try {
            const response = isActiveNow ? await UserApi.deactivate(id) : await UserApi.activate(id);
            if (response.ok) {
                await fetchUsers();
            }
        } catch (error) {
            console.error("Durum değiştirme hatası:", error);
        }
    });
};

window.handleEditUser = (user) => {
    UserUI.openModal(user);
};

window.openNewUserModal = () => {
    UserUI.openModal(null);
};

// Kaydetme/Düzenleme Form submit işlemi
window.saveUserForm = async (event) => {
    event.preventDefault();

    const id = document.getElementById("userId").value;
    const isEdit = id !== "0";
    const errorBox = document.getElementById("userModalError");

    if (errorBox) {
        errorBox.classList.add('hidden');
        errorBox.innerHTML = "";
    }

    const email = document.getElementById("userEmail").value;
    const password = document.getElementById("userPassword").value;
    const confirmPassword = document.getElementById("confirmUserPassword").value;
    const role = document.getElementById("userRole").value;

    if (isEdit) {
        // --- DÜZENLEME AKIŞI ---
        const originalRole = document.getElementById("originalUserRole").value;
        const roleChanged = (role !== originalRole);
        const passwordChanged = (password.trim() !== "");

        if (!roleChanged && !passwordChanged) {
            window.NotificationUI.show("Hiçbir değişiklik yapmadınız.", "warning");
            return;
        }

        if (passwordChanged && password !== confirmPassword) {
            UserUI.displayValidationErrors({ Message: "Şifreler birbiriyle uyuşmuyor." }, "userModalError");
            return;
        }

        try {
            if (passwordChanged) {
                const pwResponse = await UserApi.resetPassword(id, {
                    newPassword: password,
                    confirmNewPassword: confirmPassword
                });
                if (!pwResponse.ok) {
                    const errData = await pwResponse.json();
                    UserUI.displayValidationErrors(errData, "userModalError");
                    return;
                }
            }

            if (roleChanged) {
                const roleResponse = await UserApi.assignRole(id, role);
                if (!roleResponse.ok) {
                    const errData = await roleResponse.json();
                    UserUI.displayValidationErrors(errData, "userModalError");
                    return;
                }
            }

            // Kendi şifresini değiştirdiyse uyarıp güvenli çıkış yaptır
            if (passwordChanged) {
                // ÇÖZÜM: localStorage'dan kontrol sağlanıyor
                const token = localStorage.getItem("token");
                if (token) {
                    const payload = JSON.parse(atob(token.split('.')[1]));
                    const currentUserId = payload.nameid || payload.sub;
                    if (currentUserId == id) {
                        window.NotificationUI.show("Kendi şifrenizi güncellediğiniz için güvenlik amacıyla oturumunuz kapatılıyor.", "info");
                        setTimeout(() => {
                            // ÇÖZÜM: Tüm sekmelerdeki oturumu düşürmek için localStorage temizleniyor
                            localStorage.removeItem("token");
                            window.location.replace("/Login");
                        }, 2000);
                        return;
                    }
                }
            }

            UserUI.closeModal();
            await fetchUsers();

        } catch (error) {
            console.error("Hata:", error);
        }
    } else {
        // --- YENİ KAYIT AKIŞI ---
        try {
            const response = await UserApi.register({ email, password, confirmPassword, role });
            if (response.ok) {
                UserUI.closeModal();
                await fetchUsers();
            } else {
                const errorData = await response.json();
                UserUI.displayValidationErrors(errorData, "userModalError");
            }
        } catch (error) {
            console.error("Kayıt hatası:", error);
        }
    }
};