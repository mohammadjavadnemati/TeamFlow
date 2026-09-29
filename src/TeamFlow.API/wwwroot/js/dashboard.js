// ─── Guard: must be logged in ────────────────────────────────────────────────
if (!Storage.getAccessToken()) {
  window.location.href = "index.html";
}

const AVATAR_COLORS = ["#6C5CE7", "#FF6B4A", "#00C2A8", "#FFC93C", "#FF4D8D", "#4C3FC9"];
const colorFor = (seed) => AVATAR_COLORS[Math.abs(hashCode(seed)) % AVATAR_COLORS.length];
function hashCode(str) {
  let h = 0;
  for (let i = 0; i < String(str).length; i++) h = (h << 5) - h + str.charCodeAt(i) | 0;
  return h;
}

let currentWorkspaces = [];
let currentWorkspaceId = null;
let currentProjects = [];

const loader = document.getElementById("loader");
const dashScreen = document.getElementById("dash-screen");

function showLoader(text) {
  loader.querySelector("span").textContent = text || "در حال بارگذاری...";
  loader.classList.remove("hidden");
}
function hideLoader() {
  loader.classList.add("hidden");
}

// ─── Boot ────────────────────────────────────────────────────────────────────
(async function init() {
  renderUserBadge();

  try {
    currentWorkspaces = await Api.getWorkspaces();
  } catch (err) {
    toast(err.message || "خطا در دریافت فضاهای کاری", true);
    hideLoader();
    return;
  }

  if (!currentWorkspaces.length) {
    hideLoader();
    openWorkspaceModal(true);
    return;
  }

  const saved = Storage.getCurrentWorkspaceId();
  const savedStillValid = currentWorkspaces.find((w) => w.id === saved);
  currentWorkspaceId = savedStillValid ? saved : currentWorkspaces[0].id;
  Storage.setCurrentWorkspaceId(currentWorkspaceId);

  renderWorkspaceDropdown();
  await loadWorkspaceData();
  dashScreen.classList.remove("hidden");
  hideLoader();
})();

function renderUserBadge() {
  const user = Storage.getUser();
  if (!user) return;
  document.getElementById("user-avatar").textContent = initialsOf(user.firstName, user.lastName);
  document.getElementById("user-avatar").style.background = colorFor(user.email);
  document.getElementById("user-name").textContent = `${user.firstName} ${user.lastName}`;
  document.getElementById("user-email").textContent = user.email;

  const greetTitle = document.getElementById("greet-title");
  if (greetTitle) greetTitle.textContent = `سلام ${user.firstName} 👋`;
}

// ─── Workspace switching ─────────────────────────────────────────────────────
function renderWorkspaceDropdown() {
  const ws = currentWorkspaces.find((w) => w.id === currentWorkspaceId);
  document.getElementById("ws-icon").textContent = (ws?.name || "؟؟").slice(0, 2);
  document.getElementById("ws-name").textContent = ws?.name || "بدون‌نام";
  document.getElementById("ws-plan").textContent = `${ws?.memberCount ?? 0} عضو`;

  const dropdown = document.getElementById("ws-dropdown");
  dropdown.innerHTML = "";
  currentWorkspaces.forEach((w) => {
    const item = document.createElement("div");
    item.className = "ws-dropdown-item";
    item.textContent = w.name;
    item.onclick = async (e) => {
      e.stopPropagation();
      dropdown.classList.remove("open");
      if (w.id === currentWorkspaceId) return;
      currentWorkspaceId = w.id;
      Storage.setCurrentWorkspaceId(w.id);
      renderWorkspaceDropdown();
      showLoader("در حال بارگذاری فضای کاری...");
      await loadWorkspaceData();
      if (currentPage !== "dashboard") await navigate(currentPage);
      hideLoader();
    };
    dropdown.appendChild(item);
  });

  const newItem = document.createElement("div");
  newItem.className = "ws-dropdown-item new";
  newItem.textContent = "+ فضای کاری جدید";
  newItem.onclick = (e) => {
    e.stopPropagation();
    dropdown.classList.remove("open");
    openWorkspaceModal(false);
  };
  dropdown.appendChild(newItem);
}

document.getElementById("ws-switch").addEventListener("click", () => {
  document.getElementById("ws-dropdown").classList.toggle("open");
});
document.addEventListener("click", () => document.getElementById("ws-dropdown").classList.remove("open"));

// ─── Load everything for the current workspace ───────────────────────────────
async function loadWorkspaceData() {
  const [dashboardResult, projectsResult, workloadResult] = await Promise.allSettled([
    Api.getDashboard(currentWorkspaceId),
    Api.getProjects(currentWorkspaceId),
    Api.getWorkload(currentWorkspaceId),
  ]);

  if (dashboardResult.status === "fulfilled") {
    renderStats(dashboardResult.value);
    renderActivity(dashboardResult.value.recentActivities);
    document.getElementById("greet-sub").textContent =
      `${dashboardResult.value.totalProjects} پروژه · ${dashboardResult.value.totalTasks} تسک · ${dashboardResult.value.activeSprints} اسپرینت فعال`;
  } else {
    toast(dashboardResult.reason?.message || "خطا در دریافت داشبورد", true);
  }

  if (projectsResult.status === "fulfilled") {
    currentProjects = projectsResult.value;
    renderProjects(currentProjects);
    populateTaskProjectSelect(currentProjects);
  } else {
    toast(projectsResult.reason?.message || "خطا در دریافت پروژه‌ها", true);
  }

  if (workloadResult.status === "fulfilled") {
    renderWorkload(workloadResult.value);
  } else {
    document.getElementById("workload-list").innerHTML =
      `<div class="panel-empty">هنوز داده‌ی بار کاری‌ای وجود ندارد.</div>`;
  }
}

// ─── Renderers ────────────────────────────────────────────────────────────────
function renderStats(d) {
  const cards = [
    { value: d.totalProjects, label: "پروژه فعال", grad: "var(--violet),var(--violet-deep)", icon: iconProjects() },
    { value: d.activeSprints, label: "اسپرینت درحال اجرا", grad: "var(--punch),var(--pink)", icon: iconSprint() },
    { value: d.blockedTasks, label: "تسک مسدود شده", grad: "var(--sun),#E8A800", icon: iconWarn() },
    { value: d.doneTasks, label: "تسک تکمیل‌شده", grad: "var(--mint),#00A691", icon: iconCheck() },
  ];
  document.getElementById("stat-grid").innerHTML = cards.map((c) => `
    <div class="stat-card">
      <div class="stat-icon" style="background:linear-gradient(135deg,${c.grad});">${c.icon}</div>
      <div class="stat-value">${toFa(c.value)}</div>
      <div class="stat-label">${c.label}</div>
    </div>`).join("");

  const slot = document.getElementById("insight-slot");
  if (d.totalProjects === 0) {
    slot.innerHTML = `
      <div class="insight-banner info">
        <div class="insight-icon" style="background:var(--violet);">${iconInfo()}</div>
        <div class="insight-text">
          <b>هنوز پروژه‌ای نساخته‌ای</b>
          <span>برای شروع، اولین پروژه‌ی این فضای کاری را از طریق API یا صفحه‌ی پروژه‌ها بساز.</span>
        </div>
      </div>`;
  } else {
    slot.innerHTML = "";
  }
}

function renderProjects(projects) {
  const list = document.getElementById("project-list");
  if (!projects.length) {
    list.innerHTML = `<div class="panel-empty">پروژه‌ای در این فضای کاری وجود ندارد.</div>`;
    return;
  }

  const tagFor = (label) => {
    const map = {
      Excellent: ["var(--mint-tint)", "#00806E", "مسیر درست"],
      Good: ["var(--mint-tint)", "#00806E", "مسیر درست"],
      "Needs Attention": ["var(--sun-tint)", "#8A6A12", "در ریسک"],
      Critical: ["var(--punch-tint)", "#C24B2E", "عقب افتاده"],
    };
    return map[label] || ["var(--violet-tint)", "var(--violet-deep)", label];
  };

  list.innerHTML = projects.map((p) => {
    const [bg, fg, label] = tagFor(p.healthLabel);
    const score = Math.round(p.healthScore);
    return `
      <div class="project-row">
        <div class="proj-ring" style="background:conic-gradient(${p.color || "var(--violet)"} 0% ${score}%, #EFEDF7 ${score}% 100%);">
          <div class="ring-inner">${toFa(score)}٪</div>
        </div>
        <div class="proj-info">
          <div class="p-name">${escapeHtml(p.name)}</div>
          <div class="p-meta">
            <span class="proj-tag" style="background:${bg};color:${fg};">${label}</span>
            · ${toFa(p.sprintCount)} اسپرینت
          </div>
        </div>
      </div>`;
  }).join("");
}

function renderActivity(activities) {
  const list = document.getElementById("activity-list");
  if (!activities || !activities.length) {
    list.innerHTML = `<div class="panel-empty">فعالیتی ثبت نشده است.</div>`;
    return;
  }
  list.innerHTML = activities.map((a) => `
    <div class="activity-item">
      <div class="act-dot" style="background:${colorFor(a.userFullName)};">${iconActivity()}</div>
      <div>
        <div class="act-text"><b>${escapeHtml(a.userFullName)}</b> ${escapeHtml(a.description)}</div>
        <div class="act-time">${timeAgo(a.createdAt)}</div>
      </div>
    </div>`).join("");
}

function renderWorkload(data) {
  const list = document.getElementById("workload-list");
  const users = data.users || [];
  if (!users.length) {
    list.innerHTML = `<div class="panel-empty">عضوی برای نمایش بار کاری وجود ندارد.</div>`;
  } else {
    list.innerHTML = users.map((u) => {
      const total = u.activeTasks + u.blockedTasks;
      const pct = Math.min(100, total * 12); // نمایش نسبی، بدون سقف واقعی سرور
      const barColor = u.isOverloaded ? "var(--punch)" : total > 5 ? "var(--sun)" : "var(--mint)";
      return `
        <div class="workload-row">
          <div class="wl-avatar" style="background:${colorFor(u.userFullName)};">${initialsOf(...u.userFullName.split(" "))}</div>
          <div class="wl-info">
            <div class="wl-name"><span>${escapeHtml(u.userFullName)}</span><span>${u.workloadEmoji} ${u.workloadLevel}</span></div>
            <div class="wl-bar"><i style="width:${pct}%;background:${barColor};"></i></div>
          </div>
        </div>`;
    }).join("");
  }

  const slot = document.getElementById("insight-slot");
  if (slot.innerHTML === "") {
    const overloaded = users.some((u) => u.isOverloaded);
    slot.innerHTML = `
      <div class="insight-banner ${overloaded ? "warn" : "ok"}">
        <div class="insight-icon" style="background:${overloaded ? "var(--punch)" : "var(--mint)"};">${overloaded ? iconWarn() : iconCheck()}</div>
        <div class="insight-text">
          <b>${overloaded ? "هشدار بار کاری" : "وضعیت بار کاری تیم"}</b>
          <span>${escapeHtml(data.summary || "")}</span>
        </div>
      </div>`;
  }
}

// ─── Workspace creation modal ────────────────────────────────────────────────
function openWorkspaceModal(forced) {
  const modal = document.getElementById("ws-modal");
  modal.classList.remove("hidden");
  modal.dataset.forced = forced ? "1" : "0";
  hideBanner("ws-modal-banner");
  document.getElementById("ws-input-name").value = "";
}

document.getElementById("ws-modal-submit").addEventListener("click", async () => {
  const name = document.getElementById("ws-input-name").value.trim();
  if (!name) {
    showBanner("ws-modal-banner", "نام فضای کاری را وارد کنید.");
    return;
  }
  try {
    const ws = await Api.createWorkspace(name, null);
    document.getElementById("ws-modal").classList.add("hidden");
    currentWorkspaces.push(ws);
    currentWorkspaceId = ws.id;
    Storage.setCurrentWorkspaceId(ws.id);
    renderWorkspaceDropdown();
    dashScreen.classList.remove("hidden");
    showLoader("در حال بارگذاری...");
    await loadWorkspaceData();
    hideLoader();
    toast("فضای کاری ساخته شد 🎉");
  } catch (err) {
    showBanner("ws-modal-banner", err.message || "ساخت فضای کاری ناموفق بود.");
  }
});

// ─── Quick task modal ────────────────────────────────────────────────────────
function populateTaskProjectSelect(projects) {
  const select = document.getElementById("task-project");
  select.innerHTML = projects.map((p) => `<option value="${p.id}">${escapeHtml(p.name)}</option>`).join("");
}

document.getElementById("new-task-btn").addEventListener("click", () => {
  if (!currentProjects.length) {
    toast("اول باید یک پروژه در این فضای کاری بسازی.", true);
    return;
  }
  hideBanner("task-modal-banner");
  document.getElementById("task-title").value = "";
  document.getElementById("task-modal").classList.remove("hidden");
});

document.getElementById("task-modal-cancel").addEventListener("click", () => {
  document.getElementById("task-modal").classList.add("hidden");
});

document.getElementById("task-modal-submit").addEventListener("click", async () => {
  const projectId = document.getElementById("task-project").value;
  const title = document.getElementById("task-title").value.trim();
  const priority = Number(document.getElementById("task-priority").value);

  if (!title) {
    showBanner("task-modal-banner", "عنوان تسک را وارد کنید.");
    return;
  }

  try {
    await Api.createTask(currentWorkspaceId, projectId, { title, priority });
    document.getElementById("task-modal").classList.add("hidden");
    toast("تسک ساخته شد ✅");
    await loadWorkspaceData();
  } catch (err) {
    showBanner("task-modal-banner", err.message || "ساخت تسک ناموفق بود.");
  }
});

// ─── Sidebar nav (only dashboard is wired to a real page for now) ────────────
document.querySelectorAll(".nav-item").forEach((item) => {
  item.addEventListener("click", () => navigate(item.dataset.page));
});

// ─── Logout ──────────────────────────────────────────────────────────────────
document.getElementById("logout-btn").addEventListener("click", async () => {
  try { await Api.logout(); } catch (_) { /* best-effort */ }
  Storage.clear();
  window.location.href = "index.html";
});

// ─── Small utils ──────────────────────────────────────────────────────────────
function toFa(n) {
  return String(n).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[d]);
}

function timeAgo(isoDate) {
  const diffMs = Date.now() - new Date(isoDate).getTime();
  const mins = Math.floor(diffMs / 60000);
  if (mins < 1) return "همین الان";
  if (mins < 60) return `${toFa(mins)} دقیقه پیش`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${toFa(hours)} ساعت پیش`;
  const days = Math.floor(hours / 24);
  return `${toFa(days)} روز پیش`;
}

// ─── Inline icons (kept tiny, no external icon font) ─────────────────────────
function iconProjects(){ return `<svg viewBox="0 0 24 24" fill="none"><path d="M4 6h16M4 12h16M4 18h10" stroke="white" stroke-width="2" stroke-linecap="round"/></svg>`; }
function iconSprint(){ return `<svg viewBox="0 0 24 24" fill="none"><path d="M13 2L3 14h7l-1 8 11-14h-7l1-6z" stroke="white" stroke-width="2" stroke-linejoin="round"/></svg>`; }
function iconWarn(){ return `<svg viewBox="0 0 24 24" fill="none"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0ZM12 9v4M12 17h.01" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>`; }
function iconCheck(){ return `<svg viewBox="0 0 24 24" fill="none"><path d="M20 6L9 17l-5-5" stroke="white" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"/></svg>`; }
function iconInfo(){ return `<svg viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="9" stroke="white" stroke-width="2"/><path d="M12 8v.01M12 12v4" stroke="white" stroke-width="2" stroke-linecap="round"/></svg>`; }
function iconActivity(){ return `<svg viewBox="0 0 24 24" fill="none"><path d="M13 2L3 14h7l-1 8 11-14h-7l1-6z" stroke="white" stroke-width="2" stroke-linejoin="round"/></svg>`; }
