function toast(message, isError = false) {
  const el = document.getElementById("toast");
  if (!el) return;
  el.textContent = message;
  el.classList.toggle("error", isError);
  el.classList.add("show");
  clearTimeout(toast._t);
  toast._t = setTimeout(() => el.classList.remove("show"), 3200);
}

function escapeHtml(str) {
  return String(str ?? "").replace(/[&<>"']/g, (c) => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;",
  })[c]);
}

function initialsOf(firstName, lastName) {
  return `${(firstName || "")[0] || ""}${(lastName || "")[0] || ""}`;
}

function showBanner(elId, message, type = "error") {
  const el = document.getElementById(elId);
  if (!el) return;
  el.textContent = message;
  el.className = `form-banner ${type} show`;
}

function hideBanner(elId) {
  const el = document.getElementById(elId);
  if (!el) return;
  el.classList.remove("show");
}
