// ─── State & constants ───────────────────────────────────────────────────────
let currentPage = "dashboard";
const Pages = {};
const pageState = {
  projectId: null,
  sprintId: null,
  taskSprintFilter: null,
  calYear: new Date().getFullYear(),
  calMonth: new Date().getMonth() + 1,
};

const esc = escapeHtml;
const PROJECT_STATUS = { 1: "برنامه‌ریزی", 2: "فعال", 3: "متوقف", 4: "تکمیل‌شده", 5: "لغو شده" };
const ROLE_FA = { 1: "مالک", 2: "ادمین", 3: "مدیر پروژه", 4: "توسعه‌دهنده", 5: "مشاهده‌گر" };
const SPRINT_STATUS = {
  1: ["برنامه‌ریزی‌شده", "var(--violet-tint)", "var(--violet-deep)"],
  2: ["فعال", "var(--mint-tint)", "#00806E"],
  3: ["تکمیل‌شده", "var(--sun-tint)", "#8A6A12"],
};
const TASK_STATUS_FA = { Todo: "در انتظار", InProgress: "در حال انجام", Review: "بازبینی", Done: "انجام‌شده", Blocked: "مسدود" };
const TASK_STATUS_COLOR = { Todo: "#B4B2C6", InProgress: "#6C5CE7", Review: "#FFC93C", Done: "#00C2A8", Blocked: "#FF6B4A" };
const PRIORITY_FA = { Low: "کم", Medium: "متوسط", High: "بالا", Critical: "بحرانی" };
const PRIORITY_COLOR = { Low: "#00C2A8", Medium: "#6C5CE7", High: "#FFC93C", Critical: "#FF6B4A" };
const WORKLOAD_FA = { Free: "آزاد", Light: "سبک", Moderate: "متوسط", Heavy: "سنگین", Overloaded: "بیش‌ازحد" };
const MONTHS_FA = ["ژانویه", "فوریه", "مارس", "آوریل", "مه", "ژوئن", "ژوئیه", "اوت", "سپتامبر", "اکتبر", "نوامبر", "دسامبر"];

// ─── Navigation ──────────────────────────────────────────────────────────────
async function navigate(page) {
  currentPage = page;
  document.querySelectorAll(".nav-item").forEach((n) => n.classList.toggle("active", n.dataset.page === page));

  const dash = document.getElementById("view-dashboard");
  const view = document.getElementById("view-page");

  if (page === "dashboard") {
    view.classList.add("hidden");
    dash.classList.remove("hidden");
    loadWorkspaceData();
    return;
  }

  dash.classList.add("hidden");
  view.classList.remove("hidden");
  view.innerHTML = `<div class="panel-empty">در حال بارگذاری...</div>`;
  try {
    await Pages[page](view);
  } catch (err) {
    view.innerHTML = `<div class="panel-empty">${esc(err.message || "خطا در بارگذاری صفحه")}</div>`;
  }
}

// ─── Shared helpers ──────────────────────────────────────────────────────────
function pageHead(title, sub, actions = "") {
  return `<div class="page-head"><div><h1>${title}</h1><div class="greet-sub">${sub}</div></div><div class="page-actions">${actions}</div></div>`;
}
function emptyState(msg) { return `<div class="panel-empty">${msg}</div>`; }
function tag(text, bg = "var(--violet-tint)", fg = "var(--violet-deep)") {
  return `<span class="proj-tag" style="background:${bg};color:${fg};">${esc(text)}</span>`;
}
function panelBox(title, inner) {
  return `<div class="panel"><div class="panel-head"><h3>${title}</h3></div><div class="panel-body" style="padding:14px 18px;">${inner}</div></div>`;
}
function barItem(label, count, pct, color) {
  return `<div class="bar-item"><div class="bar-top"><span>${esc(label)}</span><span>${toFa(count)} · ${toFa(pct)}٪</span></div><div class="wl-bar"><i style="width:${pct}%;background:${color};"></i></div></div>`;
}
const toIso = (v) => (v ? new Date(v).toISOString() : null);
const fmtDate = (iso) => new Date(iso).toLocaleDateString("fa-IR-u-ca-gregory");
function taskToUpdateBody(t, overrides = {}) {
  return {
    title: t.title,
    description: t.description,
    status: t.status,
    priority: t.priority,
    storyPoints: t.storyPoints,
    deadline: t.deadline,
    estimatedTime: t.estimatedTime,
    actualTime: t.actualTime,
    sprintId: t.sprintId,
    assigneeId: t.assignee ? t.assignee.id : null,
    labelIds: t.labels.map((l) => l.id),
    ...overrides,
  };
}

async function resolveProject() {
  const projects = await Api.getProjects(currentWorkspaceId);
  if (!projects.length) return { projects, projectId: null };
  if (!projects.find((p) => p.id === pageState.projectId)) pageState.projectId = projects[0].id;
  return { projects, projectId: pageState.projectId };
}
function projectSelect(projects, selected) {
  return `<select class="inline-select" id="project-select">${projects
    .map((p) => `<option value="${p.id}" ${p.id === selected ? "selected" : ""}>${esc(p.name)}</option>`)
    .join("")}</select>`;
}
function onProjectChange(page) {
  const el = document.getElementById("project-select");
  if (el) el.onchange = () => { pageState.projectId = el.value; pageState.sprintId = null; navigate(page); };
}

// دکمه‌هایی که attr دارند رو به handler وصل می‌کنه؛ اگه handler مقدار false برگردونه صفحه رفرش نمی‌شه
function wireActions(view, attr, handler, page) {
  view.querySelectorAll(`[${attr}]`).forEach((btn) => {
    btn.onclick = async () => {
      try {
        const r = await handler(btn.getAttribute(attr));
        if (r !== false) navigate(page);
      } catch (e) {
        toast(e.message, true);
      }
    };
  });
}

function openFormModal({ title, sub, fields, submitLabel, onSubmit }) {
  const backdrop = document.createElement("div");
  backdrop.className = "modal-backdrop";
  backdrop.innerHTML = `
    <div class="modal-box">
      <h3>${esc(title)}</h3>
      ${sub ? `<p class="sub">${esc(sub)}</p>` : ""}
      ${fields.map((f) => `
        <div class="field">
          <label for="mf-${f.id}">${esc(f.label)}</label>
          ${f.type === "select"
            ? `<select id="mf-${f.id}">${f.options.map((o) => `<option value="${o.value}" ${String(o.value) === String(f.value) ? "selected" : ""}>${esc(o.label)}</option>`).join("")}</select>`
            : `<input id="mf-${f.id}" type="${f.type || "text"}" placeholder="${esc(f.placeholder || "")}" value="${esc(f.value || "")}">`}
        </div>`).join("")}
      <div class="form-banner error" id="mf-banner"></div>
      <div class="modal-actions">
        <button class="btn-secondary" id="mf-cancel">انصراف</button>
        <button class="btn-primary punch" id="mf-submit">${esc(submitLabel)}</button>
      </div>
    </div>`;
  document.body.appendChild(backdrop);

  const close = () => backdrop.remove();
  backdrop.querySelector("#mf-cancel").onclick = close;
  backdrop.querySelector("#mf-submit").onclick = async () => {
    const values = {};
    fields.forEach((f) => (values[f.id] = backdrop.querySelector(`#mf-${f.id}`).value));
    try {
      await onSubmit(values);
      close();
    } catch (err) {
      showBanner("mf-banner", err.message || "خطا");
    }
  };
}

// ═════════════════════════════════════════════════════════════════════════════
// پروژه‌ها
// ═════════════════════════════════════════════════════════════════════════════
Pages.projects = async (view) => {
  const projects = await Api.getProjects(currentWorkspaceId);

  view.innerHTML =
    pageHead("پروژه‌ها", `${toFa(projects.length)} پروژه در این فضای کاری`,
      `<button class="new-task-btn" id="add-project">+ پروژه جدید</button>`) +
    `<div class="panel"><div class="panel-body">${
      projects.length
        ? projects.map((p) => `
          <div class="project-row">
            <span class="color-dot" style="background:${esc(p.color)};"></span>
            <div class="proj-info">
              <div class="p-name">${esc(p.name)}</div>
              <div class="p-meta">${tag(PROJECT_STATUS[p.status] || p.statusName)} ${toFa(p.sprintCount)} اسپرینت · سلامت ${toFa(Math.round(p.healthScore))}٪ · ${esc(p.createdByName)}</div>
            </div>
            <div class="row-actions"><button class="row-btn danger" data-del="${p.id}">حذف</button></div>
          </div>`).join("")
        : emptyState("هنوز پروژه‌ای نساخته‌ای. با دکمه‌ی «پروژه جدید» شروع کن.")
    }</div></div>`;

  document.getElementById("add-project").onclick = () =>
    openFormModal({
      title: "پروژه جدید",
      fields: [
        { id: "name", label: "نام پروژه", placeholder: "مثلا: اپلیکیشن موبایل" },
        { id: "description", label: "توضیحات (اختیاری)" },
        { id: "start", label: "تاریخ شروع", type: "date" },
        { id: "end", label: "تاریخ پایان", type: "date" },
        { id: "status", label: "وضعیت", type: "select", value: 2,
          options: Object.entries(PROJECT_STATUS).map(([value, label]) => ({ value, label })) },
        { id: "color", label: "رنگ", type: "color", value: "#6C5CE7" },
      ],
      submitLabel: "ساخت پروژه",
      onSubmit: async (v) => {
        if (!v.name.trim()) throw new Error("نام پروژه را وارد کنید.");
        await Api.createProject(currentWorkspaceId, {
          name: v.name.trim(),
          description: v.description.trim() || null,
          startDate: toIso(v.start),
          endDate: toIso(v.end),
          status: Number(v.status),
          color: v.color,
        });
        toast("پروژه ساخته شد ✅");
        navigate("projects");
      },
    });

  wireActions(view, "data-del", async (id) => {
    if (!confirm("این پروژه با همه‌ی اسپرینت‌ها و تسک‌هایش حذف می‌شود. مطمئنی؟")) return false;
    await Api.deleteProject(currentWorkspaceId, id);
    toast("پروژه حذف شد");
  }, "projects");
};

// ═════════════════════════════════════════════════════════════════════════════
// اسپرینت‌ها
// ═════════════════════════════════════════════════════════════════════════════
Pages.sprints = async (view) => {
  const { projects, projectId } = await resolveProject();
  if (!projectId) {
    view.innerHTML = pageHead("اسپرینت‌ها", "") + emptyState("اول باید یک پروژه بسازی.");
    return;
  }
  const sprints = await Api.getSprints(currentWorkspaceId, projectId);

  view.innerHTML =
    pageHead("اسپرینت‌ها", `${toFa(sprints.length)} اسپرینت در این پروژه`,
      projectSelect(projects, projectId) + `<button class="new-task-btn" id="add-sprint">+ اسپرینت جدید</button>`) +
    `<div class="panel"><div class="panel-body">${
      sprints.length
        ? sprints.map((s) => {
            const [label, bg, fg] = SPRINT_STATUS[s.status];
            return `
            <div class="project-row">
              <div class="proj-info">
                <div class="p-name">${esc(s.name)}</div>
                <div class="p-meta">${tag(label, bg, fg)} ${fmtDate(s.startDate)} تا ${fmtDate(s.endDate)}${s.status !== 3 ? ` · ${toFa(s.daysRemaining)} روز مانده` : ""}${s.goal ? ` · ${esc(s.goal)}` : ""}</div>
              </div>
              <div class="row-actions">
                ${s.status === 1 ? `<button class="row-btn success" data-activate="${s.id}">فعال‌سازی</button>` : ""}
                ${s.status === 2 ? `<button class="row-btn success" data-complete="${s.id}">تکمیل</button>` : ""}
                <button class="row-btn danger" data-sdel="${s.id}">حذف</button>
              </div>
            </div>`;
          }).join("")
        : emptyState("این پروژه هنوز اسپرینتی ندارد.")
    }</div></div>`;

  onProjectChange("sprints");

  document.getElementById("add-sprint").onclick = () =>
    openFormModal({
      title: "اسپرینت جدید",
      fields: [
        { id: "name", label: "نام اسپرینت", placeholder: "مثلا: Sprint 1" },
        { id: "goal", label: "هدف (اختیاری)" },
        { id: "start", label: "تاریخ شروع", type: "date" },
        { id: "end", label: "تاریخ پایان", type: "date" },
      ],
      submitLabel: "ساخت اسپرینت",
      onSubmit: async (v) => {
        if (!v.name.trim() || !v.start || !v.end) throw new Error("نام و تاریخ شروع/پایان الزامی است.");
        await Api.createSprint(currentWorkspaceId, projectId, {
          name: v.name.trim(), goal: v.goal.trim() || null, startDate: toIso(v.start), endDate: toIso(v.end),
        });
        toast("اسپرینت ساخته شد ✅");
        navigate("sprints");
      },
    });

  wireActions(view, "data-activate", async (id) => { await Api.activateSprint(currentWorkspaceId, projectId, id); toast("اسپرینت فعال شد"); }, "sprints");
  wireActions(view, "data-complete", async (id) => { await Api.completeSprint(currentWorkspaceId, projectId, id); toast("اسپرینت تکمیل شد"); }, "sprints");
  wireActions(view, "data-sdel", async (id) => {
    if (!confirm("این اسپرینت حذف شود؟")) return false;
    await Api.deleteSprint(currentWorkspaceId, projectId, id);
    toast("اسپرینت حذف شد");
  }, "sprints");
};
// ═════════════════════════════════════════════════════════════════════════════
// تسک‌ها
// ═════════════════════════════════════════════════════════════════════════════
Pages.tasks = async (view) => {
  const w = currentWorkspaceId;
  const { projects, projectId } = await resolveProject();
  if (!projectId) {
    view.innerHTML = pageHead("تسک‌ها", "") + emptyState("اول باید یک پروژه بسازی.");
    return;
  }

  const STATUS_OPTIONS = [
    [1, "Todo", "در انتظار"],
    [2, "InProgress", "در حال انجام"],
    [3, "Review", "بازبینی"],
    [4, "Done", "انجام‌شده"],
    [5, "Blocked", "مسدود"],
  ];

  const [sprints, members, tasks] = await Promise.all([
    Api.getSprints(w, projectId),
    Api.getWorkspaceMembers(w),
    Api.getTasks(w, projectId, { sprintId: pageState.taskSprintFilter || undefined }),
  ]);

  const sprintFilterSel = `<select class="inline-select" id="sprint-filter">
    <option value="">همه اسپرینت‌ها</option>
    ${sprints.map((s) => `<option value="${s.id}" ${s.id === pageState.taskSprintFilter ? "selected" : ""}>${esc(s.name)}</option>`).join("")}
  </select>`;

  view.innerHTML =
    pageHead("تسک‌ها", `${toFa(tasks.length)} تسک در این پروژه`,
      projectSelect(projects, projectId) + sprintFilterSel +
      `<button class="new-task-btn" id="add-task">+ تسک جدید</button>`) +
    `<div class="panel"><div class="panel-body">${
      tasks.length
        ? tasks.map((t) => `
          <div class="project-row">
            <span class="color-dot" style="background:${PRIORITY_COLOR[t.priorityName] || "#6C5CE7"};" title="اولویت ${PRIORITY_FA[t.priorityName] || t.priorityName}"></span>
            <div class="proj-info">
              <div class="p-name">${esc(t.title)}</div>
              <div class="p-meta">
                ${t.assignee ? esc(t.assignee.firstName + " " + t.assignee.lastName) : "بدون مسئول"}
                ${t.deadline ? " · ددلاین " + fmtDate(t.deadline) : ""}
                ${t.sprintName ? " · " + esc(t.sprintName) : ""}
                ${t.subtaskCount ? ` · ${toFa(t.completedSubtaskCount)}/${toFa(t.subtaskCount)} زیرتسک` : ""}
              </div>
            </div>
            <div class="row-actions">
              <select class="inline-select" data-status="${t.id}" style="background:${TASK_STATUS_COLOR[t.statusName] || "#6C5CE7"}22;">
                ${STATUS_OPTIONS.map(([val, , label]) => `<option value="${val}" ${val === t.status ? "selected" : ""}>${label}</option>`).join("")}
              </select>
              <select class="inline-select" data-assignee="${t.id}">
                <option value="">بدون مسئول</option>
                ${members.map((m) => `<option value="${m.userId}" ${t.assignee && t.assignee.id === m.userId ? "selected" : ""}>${esc(m.firstName)} ${esc(m.lastName)}</option>`).join("")}
              </select>
              <button class="row-btn danger" data-tdel="${t.id}">حذف</button>
            </div>
          </div>`).join("")
        : emptyState("تسکی برای این فیلتر پیدا نشد.")
    }</div></div>`;

  onProjectChange("tasks");
  const sf = document.getElementById("sprint-filter");
  if (sf) sf.onchange = () => { pageState.taskSprintFilter = sf.value || null; navigate("tasks"); };

  document.getElementById("add-task").onclick = () =>
    openFormModal({
      title: "تسک جدید",
      fields: [
        { id: "title", label: "عنوان تسک", placeholder: "مثلا: رفع باگ صفحه ورود" },
        { id: "description", label: "توضیحات (اختیاری)" },
        { id: "priority", label: "اولویت", type: "select", value: 2,
          options: [{ value: 1, label: "کم" }, { value: 2, label: "متوسط" }, { value: 3, label: "بالا" }, { value: 4, label: "بحرانی" }] },
        { id: "deadline", label: "ددلاین (اختیاری)", type: "date" },
        { id: "sprint", label: "اسپرینت (اختیاری)", type: "select", value: "",
          options: [{ value: "", label: "بدون اسپرینت" }, ...sprints.map((s) => ({ value: s.id, label: s.name }))] },
        { id: "assignee", label: "مسئول (اختیاری)", type: "select", value: "",
          options: [{ value: "", label: "بدون مسئول" }, ...members.map((m) => ({ value: m.userId, label: `${m.firstName} ${m.lastName}` }))] },
      ],
      submitLabel: "ساخت تسک",
      onSubmit: async (v) => {
        if (!v.title.trim()) throw new Error("عنوان تسک را وارد کنید.");
        await Api.createTask(w, projectId, {
          title: v.title.trim(),
          description: v.description.trim() || null,
          priority: Number(v.priority),
          deadline: toIso(v.deadline),
          sprintId: v.sprint || null,
          assigneeId: v.assignee || null,
        });
        toast("تسک ساخته شد ✅");
        navigate("tasks");
      },
    });

  view.querySelectorAll("[data-status]").forEach((sel) => {
    sel.onchange = async () => {
      const task = tasks.find((t) => t.id === sel.getAttribute("data-status"));
      try {
        await Api.updateTask(w, projectId, task.id, taskToUpdateBody(task, { status: Number(sel.value) }));
        toast("وضعیت تسک به‌روزرسانی شد");
      } catch (e) {
        toast(e.message, true);
      }
      navigate("tasks");
    };
  });

  view.querySelectorAll("[data-assignee]").forEach((sel) => {
    sel.onchange = async () => {
      try {
        await Api.assignTask(w, projectId, sel.getAttribute("data-assignee"), sel.value || null);
        toast("مسئول تسک تغییر کرد");
      } catch (e) {
        toast(e.message, true);
      }
      navigate("tasks");
    };
  });

  wireActions(view, "data-tdel", async (id) => {
    if (!confirm("این تسک حذف شود؟")) return false;
    await Api.deleteTask(w, projectId, id);
    toast("تسک حذف شد");
  }, "tasks");
};

// ═════════════════════════════════════════════════════════════════════════════
// تقویم (ماه میلادی، چون API بر اساس ماه/سال میلادی کار می‌کنه)
// ═════════════════════════════════════════════════════════════════════════════
Pages.calendar = async (view) => {
  const y = pageState.calYear, m = pageState.calMonth;
  const events = await Api.getCalendar(currentWorkspaceId, y, m);

  const byDay = {};
  events.forEach((e) => {
    const d = new Date(e.deadline).getUTCDate();
    (byDay[d] = byDay[d] || []).push(e);
  });

  const daysInMonth = new Date(y, m, 0).getDate();
  const offset = (new Date(y, m - 1, 1).getDay() + 1) % 7; // شنبه اولین ستون
  const now = new Date();

  let cells = "";
  for (let i = 0; i < offset; i++) cells += `<div class="cal-cell empty"></div>`;
  for (let d = 1; d <= daysInMonth; d++) {
    const isToday = now.getFullYear() === y && now.getMonth() + 1 === m && now.getDate() === d;
    const chips = (byDay[d] || []).map((e) =>
      `<div class="cal-chip ${e.status === 4 ? "done" : ""}" style="background:${esc(e.projectColor)};" title="${esc(e.title)} — ${esc(e.projectName)}${e.assigneeName ? " — " + esc(e.assigneeName) : ""}">${esc(e.title)}</div>`
    ).join("");
    cells += `<div class="cal-cell ${isToday ? "today" : ""}"><div class="cal-day">${toFa(d)}</div>${chips}</div>`;
  }

  view.innerHTML =
    pageHead("تقویم ددلاین‌ها", `${MONTHS_FA[m - 1]} ${toFa(y)} · ${toFa(events.length)} تسک با ددلاین`,
      `<button class="row-btn" id="cal-prev">ماه قبل</button><button class="row-btn" id="cal-next">ماه بعد</button>`) +
    `<div class="cal-grid">${["ش", "ی", "د", "س", "چ", "پ", "ج"].map((d) => `<div class="cal-head">${d}</div>`).join("")}${cells}</div>`;

  const shift = (delta) => {
    let nm = pageState.calMonth + delta, ny = pageState.calYear;
    if (nm < 1) { nm = 12; ny--; }
    if (nm > 12) { nm = 1; ny++; }
    pageState.calMonth = nm; pageState.calYear = ny;
    navigate("calendar");
  };
  document.getElementById("cal-prev").onclick = () => shift(-1);
  document.getElementById("cal-next").onclick = () => shift(1);
};

// ═════════════════════════════════════════════════════════════════════════════
// آنالیتیکس
// ═════════════════════════════════════════════════════════════════════════════
Pages.analytics = async (view) => {
  const w = currentWorkspaceId;
  const { projects, projectId } = await resolveProject();
  const [perUser, rate, byStatus, byPriority] = await Promise.all([
    Api.getTasksPerUser(w),
    projectId ? Api.getCompletionRate(w, projectId) : null,
    projectId ? Api.getTasksPerStatus(w, projectId) : [],
    projectId ? Api.getTasksPerPriority(w, projectId) : [],
  ]);

  const overloaded = perUser.filter((u) => u.isOverloaded).length;
  const stat = (value, label, grad) =>
    `<div class="stat-card"><div class="stat-icon" style="background:linear-gradient(135deg,${grad});"></div><div class="stat-value">${value}</div><div class="stat-label">${label}</div></div>`;

  let html = pageHead("آنالیتیکس", "آمار پروژه و عملکرد اعضا", projectId ? projectSelect(projects, projectId) : "");

  if (projectId) {
    html += `<div class="stat-grid">
      ${stat(toFa(rate.rate) + "٪", "نرخ تکمیل پروژه", "var(--mint),#00A691")}
      ${stat(toFa(rate.totalTasks), "کل تسک‌ها", "var(--violet),var(--violet-deep)")}
      ${stat(toFa(rate.completedTasks), "تسک‌های انجام‌شده", "var(--sun),#E8A800")}
      ${stat(toFa(overloaded), "عضو بیش‌ازحد مشغول", "var(--punch),var(--pink)")}
    </div>
    <div class="grid-2col" style="margin-bottom:20px;">
      ${panelBox("تسک‌ها بر اساس وضعیت", byStatus.map((s) => barItem(TASK_STATUS_FA[s.status] || s.status, s.count, s.percentage, TASK_STATUS_COLOR[s.status] || "#6C5CE7")).join(""))}
      ${panelBox("تسک‌ها بر اساس اولویت", byPriority.map((s) => barItem(PRIORITY_FA[s.priority] || s.priority, s.count, s.percentage, PRIORITY_COLOR[s.priority] || "#6C5CE7")).join(""))}
    </div>`;
  } else {
    html += emptyState("برای دیدن آمار پروژه، اول یک پروژه بساز.");
  }

  html += panelBox("تسک‌ها به تفکیک عضو", perUser.length
    ? `<div class="table-wrap"><table class="data-table"><thead><tr><th>عضو</th><th>کل</th><th>در حال انجام</th><th>انجام‌شده</th><th>مسدود</th><th>وضعیت</th></tr></thead><tbody>${
        perUser.map((u) => `<tr><td>${esc(u.userFullName)}</td><td>${toFa(u.totalTasks)}</td><td>${toFa(u.inProgressTasks)}</td><td>${toFa(u.completedTasks)}</td><td>${toFa(u.blockedTasks)}</td><td>${u.isOverloaded ? tag("بیش‌ازحد مشغول", "var(--punch-tint)", "#C24B2E") : tag("عادی", "var(--mint-tint)", "#00806E")}</td></tr>`).join("")
      }</tbody></table></div>`
    : emptyState("داده‌ای وجود ندارد."));

  view.innerHTML = html;
  onProjectChange("analytics");
};

// ═════════════════════════════════════════════════════════════════════════════
// اعضای تیم
// ═════════════════════════════════════════════════════════════════════════════
Pages.members = async (view) => {
  const w = currentWorkspaceId;
  const members = await Api.getWorkspaceMembers(w);
  const me = Storage.getUser();

  view.innerHTML =
    pageHead("اعضای تیم", `${toFa(members.length)} عضو`, `<button class="new-task-btn" id="invite-member">+ دعوت عضو</button>`) +
    `<div class="panel"><div class="panel-body">${members.map((m) => `
      <div class="project-row">
        <div class="wl-avatar" style="background:${colorFor(m.email)};">${esc(initialsOf(m.firstName, m.lastName))}</div>
        <div class="proj-info">
          <div class="p-name">${esc(m.firstName)} ${esc(m.lastName)}${m.userId === me?.id ? " (شما)" : ""}</div>
          <div class="p-meta">${esc(m.email)}</div>
        </div>
        <div class="row-actions">
          ${m.role === 1
            ? tag("مالک")
            : `<select class="inline-select" data-role="${m.userId}">${[2, 3, 4, 5].map((r) => `<option value="${r}" ${r === m.role ? "selected" : ""}>${ROLE_FA[r]}</option>`).join("")}</select>
               <button class="row-btn danger" data-remove="${m.userId}">حذف</button>`}
        </div>
      </div>`).join("")}</div></div>`;

  document.getElementById("invite-member").onclick = () =>
    openFormModal({
      title: "دعوت عضو جدید",
      sub: "کاربر باید قبلاً در TeamFlow ثبت‌نام کرده باشد.",
      fields: [
        { id: "email", label: "ایمیل", type: "email", placeholder: "user@company.com" },
        { id: "role", label: "نقش", type: "select", value: 4,
          options: [2, 3, 4, 5].map((r) => ({ value: r, label: ROLE_FA[r] })) },
      ],
      submitLabel: "ارسال دعوت",
      onSubmit: async (v) => {
        if (!v.email.trim()) throw new Error("ایمیل را وارد کنید.");
        await Api.inviteMember(w, v.email.trim(), Number(v.role));
        toast("عضو اضافه شد ✅");
        navigate("members");
      },
    });

  view.querySelectorAll("[data-role]").forEach((sel) => {
    sel.onchange = async () => {
      try {
        await Api.updateMemberRole(w, sel.getAttribute("data-role"), Number(sel.value));
        toast("نقش تغییر کرد");
      } catch (e) {
        toast(e.message, true);
        navigate("members");
      }
    };
  });

  wireActions(view, "data-remove", async (id) => {
    if (!confirm("این عضو از فضای کاری حذف شود؟")) return false;
    await Api.removeMember(w, id);
    toast("عضو حذف شد");
  }, "members");
};

// ═════════════════════════════════════════════════════════════════════════════
// بار کاری تیم
// ═════════════════════════════════════════════════════════════════════════════
Pages.workload = async (view) => {
  const w = currentWorkspaceId;
  const [wl, prod] = await Promise.all([Api.getWorkload(w), Api.getProductivity(w)]);
  const anyOver = wl.users.some((u) => u.isOverloaded);

  view.innerHTML =
    pageHead("بار کاری تیم", "تحلیل توزیع کار و بهره‌وری اعضا") +
    `<div class="insight-banner ${anyOver ? "warn" : "ok"}">
      <div class="insight-text"><b>${anyOver ? "هشدار بار کاری" : "وضعیت بار کاری"}</b><span>${esc(wl.summary)}</span></div>
    </div>` +
    panelBox("بار کاری فعلی", wl.users.length
      ? `<div class="table-wrap"><table class="data-table"><thead><tr><th>عضو</th><th>سطح</th><th>در حال انجام</th><th>مسدود</th><th>معوق</th></tr></thead><tbody>${
          wl.users.map((u) => `<tr><td>${esc(u.userFullName)}</td><td>${u.workloadEmoji} ${WORKLOAD_FA[u.workloadLevel] || u.workloadLevel}</td><td>${toFa(u.activeTasks)}</td><td>${toFa(u.blockedTasks)}</td><td>${toFa(u.overdueTasks)}</td></tr>`).join("")
        }</tbody></table></div>`
      : emptyState("عضوی وجود ندارد.")) +
    `<div style="height:20px;"></div>` +
    panelBox("امتیاز بهره‌وری اعضا", prod.length
      ? prod.map((p) => barItem(`${p.userFullName} — ${p.insight}`, p.completedTasks, Math.round(p.score), p.score >= 60 ? "var(--mint)" : p.score >= 40 ? "var(--sun)" : "var(--punch)")).join("") +
        `<div class="greet-sub" style="margin-top:6px;">عدد کنار هر نفر: تعداد تسک انجام‌شده · درصد: امتیاز بهره‌وری</div>`
      : emptyState("داده‌ای وجود ندارد."));
};

// ═════════════════════════════════════════════════════════════════════════════
// ریسک ددلاین
// ═════════════════════════════════════════════════════════════════════════════
Pages.risk = async (view) => {
  const w = currentWorkspaceId;
  const { projects, projectId } = await resolveProject();
  if (!projectId) {
    view.innerHTML = pageHead("ریسک ددلاین", "") + emptyState("اول باید یک پروژه و اسپرینت بسازی.");
    return;
  }

  const sprints = await Api.getSprints(w, projectId);
  const head = (extra = "") => pageHead("ریسک ددلاین", "پیش‌بینی اینکه اسپرینت به‌موقع تمام می‌شود یا نه", projectSelect(projects, projectId) + extra);

  if (!sprints.length) {
    view.innerHTML = head() + emptyState("این پروژه اسپرینتی ندارد.");
    onProjectChange("risk");
    return;
  }

  if (!sprints.find((s) => s.id === pageState.sprintId)) {
    pageState.sprintId = (sprints.find((s) => s.status === 2) || sprints[0]).id;
  }
  const sprintSel = `<select class="inline-select" id="sprint-select">${sprints.map((s) => `<option value="${s.id}" ${s.id === pageState.sprintId ? "selected" : ""}>${esc(s.name)}</option>`).join("")}</select>`;

  const r = await Api.getDeadlineRisk(w, projectId, pageState.sprintId);

  view.innerHTML =
    head(sprintSel) +
    `<div class="insight-banner ${r.isAtRisk ? "warn" : "ok"}">
      <div class="insight-text"><b>${r.riskEmoji} سطح ریسک: ${esc(r.riskLevel)}</b><span>${esc(r.message)}</span></div>
    </div>` +
    panelBox("پیشرفت اسپرینت",
      barItem("پیشرفت فعلی", 0, r.currentProgress, "var(--mint)").replace(`${toFa(0)} · `, "") +
      barItem("پیشرفت لازم تا امروز", 0, r.requiredProgress, "var(--violet)").replace(`${toFa(0)} · `, "") +
      `<div class="greet-sub">${toFa(r.daysRemaining)} روز تا پایان اسپرینت</div>`) +
    `<div style="height:20px;"></div>` +
    panelBox("تسک‌های پرریسک (ددلاین تا ۳ روز آینده یا معوق)", r.riskyTasks.length
      ? r.riskyTasks.map((t) => `
        <div class="project-row">
          <div class="proj-info">
            <div class="p-name">${esc(t.title)}</div>
            <div class="p-meta">${t.assigneeName ? esc(t.assigneeName) : "بدون مسئول"} · ددلاین ${fmtDate(t.deadline)} · ${t.daysUntilDeadline < 0 ? tag("معوق", "var(--punch-tint)", "#C24B2E") : toFa(t.daysUntilDeadline) + " روز مانده"}</div>
          </div>
        </div>`).join("")
      : emptyState("تسک پرریسکی وجود ندارد 🎉"));

  onProjectChange("risk");
  const ss = document.getElementById("sprint-select");
  if (ss) ss.onchange = () => { pageState.sprintId = ss.value; navigate("risk"); };
};