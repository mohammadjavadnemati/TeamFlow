// ─── Token storage ───────────────────────────────────────────────────────────

const Storage = {
  getAccessToken: () => localStorage.getItem("tf_access_token"),
  getRefreshToken: () => localStorage.getItem("tf_refresh_token"),
  getUser: () => JSON.parse(localStorage.getItem("tf_user") || "null"),
  getCurrentWorkspaceId: () => localStorage.getItem("tf_current_workspace"),

  setSession(authResponse) {
    localStorage.setItem("tf_access_token", authResponse.accessToken);
    localStorage.setItem("tf_refresh_token", authResponse.refreshToken);
    localStorage.setItem("tf_user", JSON.stringify(authResponse.user));
  },

  setCurrentWorkspaceId(id) {
    localStorage.setItem("tf_current_workspace", id);
  },

  clear() {
    localStorage.removeItem("tf_access_token");
    localStorage.removeItem("tf_refresh_token");
    localStorage.removeItem("tf_user");
    localStorage.removeItem("tf_current_workspace");
  },
};

// ─── Low-level request helper ────────────────────────────────────────────────

class ApiError extends Error {
  constructor(message, status, errors) {
    super(message);
    this.status = status;
    this.errors = errors || [];
  }
}

let refreshInFlight = null;

async function rawRequest(path, options = {}, withAuth = true) {
  const headers = { "Content-Type": "application/json", ...(options.headers || {}) };

  if (withAuth) {
    const token = Storage.getAccessToken();
    if (token) headers["Authorization"] = `Bearer ${token}`;
  }

  const res = await fetch(`${CONFIG.API_BASE_URL}${path}`, { ...options, headers });

  let body = null;
  try {
    body = await res.json();
  } catch (_) {
    /* بدنه خالی، مثلا در بعضی پاسخ‌های 204 */
  }

  return { res, body };
}

// اگر accessToken منقضی شده (401) یک‌بار با refreshToken تلاش می‌کنیم و درخواست را تکرار می‌کنیم
async function request(path, options = {}, withAuth = true) {
  let { res, body } = await rawRequest(path, options, withAuth);

  if (res.status === 401 && withAuth && Storage.getRefreshToken()) {
    if (!refreshInFlight) {
      refreshInFlight = rawRequest(
        "/auth/refresh-token",
        { method: "POST", body: JSON.stringify({ refreshToken: Storage.getRefreshToken() }) },
        false
      ).finally(() => (refreshInFlight = null));
    }

    const refreshResult = await refreshInFlight;
    if (refreshResult.res.ok && refreshResult.body?.data) {
      Storage.setSession(refreshResult.body.data);
      ({ res, body } = await rawRequest(path, options, withAuth));
    } else {
      Storage.clear();
      window.location.href = "index.html";
      throw new ApiError("نشست شما منقضی شده. دوباره وارد شوید.", 401);
    }
  }

  if (!res.ok || body?.success === false) {
    const message = body?.message || `خطای سرور (${res.status})`;
    throw new ApiError(message, res.status, body?.errors);
  }

  return body?.data;
}

// ─── Auth ────────────────────────────────────────────────────────────────────

const Api = {
  register: (firstName, lastName, email, password) =>
    request("/auth/register", { method: "POST", body: JSON.stringify({ firstName, lastName, email, password }) }, false),

  login: (email, password) =>
    request("/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }, false),

  logout: () =>
    request("/auth/logout", { method: "POST", body: JSON.stringify({ refreshToken: Storage.getRefreshToken() }) }),

  me: () => request("/auth/me"),

  // ─── Workspaces ─────────────────────────────────────────────────────────────
  getWorkspaces: () => request("/workspaces"),
  createWorkspace: (name, description) =>
    request("/workspaces", { method: "POST", body: JSON.stringify({ name, description, logoUrl: null }) }),
  getWorkspaceMembers: (workspaceId) => request(`/workspaces/${workspaceId}/members`),

  // ─── Dashboard ──────────────────────────────────────────────────────────────
  getDashboard: (workspaceId) => request(`/workspaces/${workspaceId}/dashboard`),

  // ─── Projects ───────────────────────────────────────────────────────────────
  getProjects: (workspaceId) => request(`/workspaces/${workspaceId}/projects`),
  createProject: (workspaceId, body) =>
    request(`/workspaces/${workspaceId}/projects`, { method: "POST", body: JSON.stringify(body) }),
  deleteProject: (workspaceId, projectId) =>
    request(`/workspaces/${workspaceId}/projects/${projectId}`, { method: "DELETE" }),

  // ─── Activity ───────────────────────────────────────────────────────────────
  getWorkspaceActivities: (workspaceId, pageSize = 6) =>
    request(`/workspaces/${workspaceId}/activities?page=1&pageSize=${pageSize}`),

  // ─── Smart ──────────────────────────────────────────────────────────────────
  getWorkload: (workspaceId) => request(`/workspaces/${workspaceId}/smart/workload`),

  // ─── Tasks ──────────────────────────────────────────────────────────────────
  getTasks: (w, p, filter = {}) => {
    const qs = new URLSearchParams(Object.entries(filter).filter(([, v]) => v !== null && v !== undefined && v !== "")).toString();
    return request(`/workspaces/${w}/projects/${p}/tasks${qs ? `?${qs}` : ""}`);
  },
  createTask: (w, p, body) =>
    request(`/workspaces/${w}/projects/${p}/tasks`, { method: "POST", body: JSON.stringify(body) }),
  updateTask: (w, p, taskId, body) =>
    request(`/workspaces/${w}/projects/${p}/tasks/${taskId}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteTask: (w, p, taskId) => request(`/workspaces/${w}/projects/${p}/tasks/${taskId}`, { method: "DELETE" }),
  assignTask: (w, p, taskId, assigneeId) =>
    request(`/workspaces/${w}/projects/${p}/tasks/${taskId}/assign`, { method: "PATCH", body: JSON.stringify({ assigneeId }) }),
  // ─── Sprints ────────────────────────────────────────────────────────────────
  getSprints: (w, p) => request(`/workspaces/${w}/projects/${p}/sprints`),
  createSprint: (w, p, body) =>
    request(`/workspaces/${w}/projects/${p}/sprints`, { method: "POST", body: JSON.stringify(body) }),
  activateSprint: (w, p, s) => request(`/workspaces/${w}/projects/${p}/sprints/${s}/activate`, { method: "POST" }),
  completeSprint: (w, p, s) => request(`/workspaces/${w}/projects/${p}/sprints/${s}/complete`, { method: "POST" }),
  deleteSprint: (w, p, s) => request(`/workspaces/${w}/projects/${p}/sprints/${s}`, { method: "DELETE" }),

  // ─── Calendar ───────────────────────────────────────────────────────────────
  getCalendar: (w, year, month) => request(`/workspaces/${w}/calendar?year=${year}&month=${month}`),

  // ─── Analytics ──────────────────────────────────────────────────────────────
  getTasksPerUser: (w) => request(`/workspaces/${w}/analytics/tasks-per-user`),
  getCompletionRate: (w, p) => request(`/workspaces/${w}/projects/${p}/analytics/completion-rate`),
  getTasksPerStatus: (w, p) => request(`/workspaces/${w}/projects/${p}/analytics/tasks-per-status`),
  getTasksPerPriority: (w, p) => request(`/workspaces/${w}/projects/${p}/analytics/tasks-per-priority`),

  // ─── Members ────────────────────────────────────────────────────────────────
  inviteMember: (w, email, role) =>
    request(`/workspaces/${w}/members`, { method: "POST", body: JSON.stringify({ email, role }) }),
  updateMemberRole: (w, userId, role) =>
    request(`/workspaces/${w}/members/${userId}/role`, { method: "PUT", body: JSON.stringify({ role }) }),
  removeMember: (w, userId) => request(`/workspaces/${w}/members/${userId}`, { method: "DELETE" }),

  // ─── Smart ──────────────────────────────────────────────────────────────────
  getProductivity: (w) => request(`/workspaces/${w}/smart/productivity`),
  getDeadlineRisk: (w, p, s) => request(`/workspaces/${w}/smart/projects/${p}/sprints/${s}/risk`),
};
