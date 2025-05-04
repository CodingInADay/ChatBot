// متغیر جهانی برای ذخیره پرس‌وجوی فعلی
window.currentQuery = '';

// تابع ارسال پرس‌وجو
function sendQuery() {
    const queryInput = document.getElementById('queryInput');
    const chatMessages = document.getElementById('chatMessages');
    const query = queryInput.value.trim();
    if (!query) return;

    window.currentQuery = query;
    chatMessages.innerHTML = `<div class="chat-message user"><div class="bubble">${query}</div></div>`;
    queryInput.value = '';
    chatMessages.scrollTop = chatMessages.scrollHeight;

    const queryButton = document.getElementById('queryButton'); // ID دکمه ارسال
    if (queryButton) queryButton.disabled = true;
    fetch('/Home/Query', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `query=${encodeURIComponent(query)}&usedIds=${encodeURIComponent('[]')}`
    }).then(response => {
        console.log(`Query response status: ${response.status}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        return response.text();
    }).then(data => {
        console.log(`Query response data: ${data}`);
        chatMessages.innerHTML += data;
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }).catch(error => {
        console.error('Query error:', error);
        alert('خطا در ارتباط با سرور: ' + error.message);
    }).finally(() => {
        if (queryButton) queryButton.disabled = false;
    });
}

// تابع دریافت پاسخ‌های بیشتر
function getMore(event) {
    event.preventDefault();
    const chatMessages = document.getElementById('chatMessages');
    const usedIds = document.getElementById('usedIds').value;
    const query = window.currentQuery;
    if (!query) return;

    chatMessages.innerHTML = `<div class="chat-message user"><div class="bubble">${query}</div></div>`;
    fetch('/Home/More', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `query=${encodeURIComponent(query)}&usedIds=${encodeURIComponent(usedIds)}`
    }).then(response => {
        console.log(`More response status: ${response.status}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        return response.text();
    }).then(data => {
        console.log(`More response data: ${data}`);
        chatMessages.innerHTML += data;
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }).catch(error => {
        console.error('More error:', error);
        alert('خطا در ارتباط با سرور!');
    });
}

// تابع ثبت بازخورد
function submitFeedback(event, knowledgeId, rating) {
    event.preventDefault();
    fetch('/Home/Feedback', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `knowledgeId=${knowledgeId}&rating=${rating}`
    }).then(response => {
        console.log(`Feedback response status: ${response.status}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        return response.text();
    }).then(data => {
        console.log(`Feedback response data: ${data}`);
        if (data === 'success') {
            const button = event.target;
            button.classList.add(rating > 0 ? 'voted' : 'voted-negative');
            button.disabled = true;
            alert('بازخورد ثبت شد!');
        } else if (data === 'already_voted') {
            alert('شما قبلاً بازخورد داده‌اید!');
        } else {
            alert('خطا در ثبت بازخورد!');
        }
    }).catch(error => {
        console.error('Feedback error:', error);
        alert('خطا در ارتباط با سرور!');
    });
}

// تابع آموزش سیستم (ثبت جمله جدید)
function trainSystem() {
    const sentence = document.getElementById('trainSentence').value.trim();
    const keywords = document.getElementById('trainKeywords').value.trim();
    if (!sentence || !keywords) {
        alert('لطفاً جمله و کلمات ضروری را وارد کنید!');
        return;
    }
    const synonyms = document.getElementById('trainSynonyms').value.trim();
    const trainButton = document.getElementById('trainButton'); // ID دکمه ذخیره
    if (trainButton) trainButton.disabled = true; // غیرفعال کردن دکمه
    fetch('/Home/Train', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `sentence=${encodeURIComponent(sentence)}&keywords=${encodeURIComponent(keywords)}&synonyms=${encodeURIComponent(synonyms)}`
    }).then(response => {
        console.log(`Train response status: ${response.status}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        return response.text();
    }).then(data => {
        const cleanedData = data.trim().replace(/^"|"$/g, '').replace(/^"|"$/g, '');
        console.log(`Train response data (raw): "${data}"`);
        console.log(`Train response data (cleaned): "${cleanedData}"`);
        if (cleanedData === 'success') {
            alert('جمله ذخیره شد!');
            document.getElementById('trainSentence').value = '';
            document.getElementById('trainKeywords').value = '';
            document.getElementById('trainSynonyms').value = '';
            window.location.reload(true);
        } else {
            console.error(`Train failed, response: "${cleanedData}"`);
            alert('خطا در ذخیره جمله! پاسخ سرور: ' + cleanedData);
        }
    }).catch(error => {
        console.error('Train error:', error);
        alert('خطا در ارتباط با سرور: ' + error.message);
    }).finally(() => {
        if (trainButton) trainButton.disabled = false; // فعال کردن دکمه
    });
}

// تابع تولید جمله
function generateSentence() {
    const startPhrase = document.getElementById('startPhrase').value.trim();
    fetch('/Home/Generate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `startPhrase=${encodeURIComponent(startPhrase)}`
    }).then(response => {
        console.log(`Generate response status: ${response.status}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        return response.json();
    }).then(data => {
        console.log(`Generate response data:`, data);
        const div = document.getElementById('generatedSentence');
        div.innerHTML = `<p>${data.sentence}</p><button onclick="approveSentence('${data.sentence}')">@(ViewBag.Lang == "fa" ? "تایید" : "Approve")</button>`;
    }).catch(error => {
        console.error('Generate error:', error);
        alert('خطا در تولید جمله!');
    });
}

// تابع تایید جمله تولیدشده
function approveSentence(sentence) {
    fetch('/Home/Approve', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `sentence=${encodeURIComponent(sentence)}`
    }).then(response => {
        console.log(`Approve response status: ${response.status}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        return response.text();
    }).then(data => {
        console.log(`Approve response data: ${data}`);
        if (data === 'success') {
            document.getElementById('trainSentence').value = sentence;
            document.getElementById('trainSentence').scrollIntoView({ behavior: 'smooth' });
            alert('جمله تایید شد و به عنوان AI ثبت شد!');
            location.reload();
        } else {
            alert('خطا در تایید جمله!');
        }
    }).catch(error => {
        console.error('Approve error:', error);
        alert('خطا در ارتباط با سرور!');
    });
}

// تابع ورود
function login() {
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    const loginButton = document.getElementById('loginButton'); // ID دکمه ورود
    if (loginButton) loginButton.disabled = true; // غیرفعال کردن دکمه
    console.log(`Login - Sending: username=${username}, password=${password}`);
    fetch('/Home/Login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `username=${encodeURIComponent(username)}&password=${encodeURIComponent(password)}`
    }).then(response => {
        console.log(`Login response status: ${response.status}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        return response.text();
    }).then(data => {
        // حذف فاصله‌ها و نقل‌قول‌های اضافی
        const cleanedData = data.trim().replace(/^"|"$/g, '').replace(/^"|"$/g, '');
        console.log(`Login response data (raw): "${data}"`);
        console.log(`Login response data (cleaned): "${cleanedData}"`);
        if (cleanedData === 'success') {
            console.log('Login successful, reloading page...');
            window.location.reload(true); // رفرش اجباری
        } else {
            console.error(`Login failed, response: "${cleanedData}"`);
            alert('ورود ناموفق! پاسخ سرور: ' + cleanedData);
        }
    }).catch(error => {
        console.error('Login error:', error);
        alert('خطا در ارتباط با سرور: ' + error.message);
    }).finally(() => {
        if (loginButton) loginButton.disabled = false; // فعال کردن مجدد دکمه
    });
}
// تابع تغییر رمز
function changePass() {
    const newPass = document.getElementById('newPass').value;
    if (!newPass) {
        alert('لطفاً رمز جدید را وارد کنید!');
        return;
    }
    fetch('/Home/ChangePass', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `newPass=${encodeURIComponent(newPass)}`
    }).then(response => {
        console.log(`ChangePass response status: ${response.status}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        return response.text();
    }).then(data => {
        console.log(`ChangePass response data: ${data}`);
        if (data === 'success') {
            alert('رمز تغییر کرد!');
            location.reload();
        } else {
            alert('خطا در تغییر رمز!');
        }
    }).catch(error => {
        console.error('ChangePass error:', error);
        alert('خطا در ارتباط با سرور!');
    });
}

// تابع خروج
function logout() {
    fetch('/Home/Logout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: ''
    }).then(response => {
        console.log(`Logout response status: ${response.status}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        return response.text();
    }).then(data => {
        console.log(`Logout response data: ${data}`);
        if (data === 'success') {
            sessionStorage.clear();
            location.reload();
        } else {
            alert('خطا در خروج!');
        }
    }).catch(error => {
        console.error('Logout error:', error);
        alert('خطا در ارتباط با سرور!');
    });
}

// تابع نمایش مدال ورود/تغییر رمز
function showLogin() {
    document.getElementById('loginModal').style.display = 'block';
}

// تابع بستن مدال
function closeModal(event) {
    if (event.target.classList.contains('modal')) {
        event.target.style.display = 'none';
    }
}

// تابع تغییر تم (روشن/تیره)
function toggleTheme() {
    document.body.classList.toggle('light');
    localStorage.setItem('theme', document.body.classList.contains('light') ? 'light' : 'dark');
}

// تابع نمایش مدال ویرایش
function showEditModal(id, content, keywords, synonyms) {
    document.getElementById('editId').value = id;
    document.getElementById('editSentence').value = content;
    document.getElementById('editKeywords').value = keywords;
    document.getElementById('editSynonyms').value = synonyms;
    document.getElementById('editModal').style.display = 'block';
}

// تابع ویرایش جمله
function editKnowledge() {
    const id = document.getElementById('editId').value;
    const sentence = document.getElementById('editSentence').value.trim();
    const keywords = document.getElementById('editKeywords').value.trim();
    if (!sentence || !keywords) {
        alert('لطفاً جمله و کلمات ضروری را وارد کنید!');
        return;
    }
    const synonyms = document.getElementById('editSynonyms').value.trim();
    const editButton = document.getElementById('editButton');
    if (editButton) editButton.disabled = true;
    fetch('/Home/Edit', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `id=${id}&sentence=${encodeURIComponent(sentence)}&keywords=${encodeURIComponent(keywords)}&synonyms=${encodeURIComponent(synonyms)}`
    }).then(response => {
        console.log(`Edit response status: ${response.status}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        return response.text();
    }).then(data => {
        const cleanedData = data.trim().replace(/^"|"$/g, '').replace(/^"|"$/g, '');
        console.log(`Edit response data (raw): "${data}"`);
        console.log(`Edit response data (cleaned): "${cleanedData}"`);
        if (cleanedData === 'success') {
            alert('جمله ویرایش شد!');
            window.location.reload(true);
        } else {
            console.error(`Edit failed, response: "${cleanedData}"`);
            alert('خطا در ویرایش جمله! پاسخ سرور: ' + cleanedData);
        }
    }).catch(error => {
        console.error('Edit error:', error);
        alert('خطا در ارتباط با سرور: ' + error.message);
    }).finally(() => {
        if (editButton) editButton.disabled = false;
    });
}

function deleteKnowledge(id) {
    if (confirm('آیا مطمئنید که می‌خواهید این جمله را حذف کنید؟')) {
        const deleteButton = document.querySelector(`button[onclick="deleteKnowledge(${id})"]`);
        if (deleteButton) deleteButton.disabled = true;
        fetch('/Home/Delete', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `id=${id}`
        }).then(response => {
            console.log(`Delete response status: ${response.status}`);
            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
            return response.text();
        }).then(data => {
            const cleanedData = data.trim().replace(/^"|"$/g, '').replace(/^"|"$/g, '');
            console.log(`Delete response data (raw): "${data}"`);
            console.log(`Delete response data (cleaned): "${cleanedData}"`);
            if (cleanedData === 'success') {
                alert('جمله حذف شد!');
                window.location.reload(true);
            } else {
                console.error(`Delete failed, response: "${cleanedData}"`);
                alert('خطا در حذف جمله! پاسخ سرور: ' + cleanedData);
            }
        }).catch(error => {
            console.error('Delete error:', error);
            alert('خطا در ارتباط با سرور: ' + error.message);
        }).finally(() => {
            if (deleteButton) deleteButton.disabled = false;
        });
    }
}

// تابع جستجو در جملات
function searchKnowledge() {
    const search = document.getElementById('knowledgeSearch').value.trim();
    location.href = `?lang=${document.querySelector('select').value}&search=${encodeURIComponent(search)}`;
}

// تابع تغییر صفحه در جدول
function changePage(page) {
    if (page < 1) return;
    location.href = `?lang=${document.querySelector('select').value}&page=${page}&search=${encodeURIComponent(document.getElementById('knowledgeSearch').value)}&sort=${getSortParam()}&order=${getOrderParam()}`;
}

// تابع مرتب‌سازی جدول
function sortTable(column) {
    const currentOrder = getOrderParam();
    const newOrder = currentOrder === 'ASC' ? 'DESC' : 'ASC';
    location.href = `?lang=${document.querySelector('select').value}&sort=${column}&order=${newOrder}&search=${encodeURIComponent(document.getElementById('knowledgeSearch').value)}&page=${getPageParam()}`;
}

// تابع‌های کمکی برای دریافت پارامترهای URL
function getSortParam() {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get('sort') || 'timestamp';
}

function getOrderParam() {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get('order') || 'DESC';
}

function getPageParam() {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get('page') || '1';
}

// کد اجرایی هنگام لود صفحه
document.addEventListener('DOMContentLoaded', () => {
    // اعمال تم ذخیره‌شده
    const theme = localStorage.getItem('theme');
    if (theme === 'light') {
        document.body.classList.add('light');
    }

    // افزودن رویداد برای ارسال پرس‌وجو با کلید Enter
    document.getElementById('queryInput').addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            sendQuery();
        }
    });

    // افزودن رویداد برای جستجو در جدول با کلید Enter
    const knowledgeSearch = document.getElementById('knowledgeSearch');
    if (knowledgeSearch) {
        knowledgeSearch.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                searchKnowledge();
            }
        });
    }
});