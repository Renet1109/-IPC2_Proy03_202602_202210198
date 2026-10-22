"use strict";
document.querySelectorAll("form[data-confirm]").forEach(form => form.addEventListener("submit", event => {
    if (!window.confirm(form.dataset.confirm)) event.preventDefault();
}));
const input = document.getElementById("Archivos");
const previews = document.getElementById("previews");
if (input && previews) input.addEventListener("change", async () => {
    previews.replaceChildren();
    for (const file of input.files) {
        const box = document.createElement("article"); box.className = "preview";
        const title = document.createElement("h3"); title.textContent = file.name; box.append(title);
        const description = document.createElement("p"); box.append(description); previews.append(box);
        if (file.size > 5 * 1024 * 1024) { description.textContent = "El archivo excede 5 MB."; continue; }
        try {
            const data = JSON.parse(await file.text());
            const first = Array.isArray(data) ? data[0] : null;
            const type = first && typeof first === "object" && "NIT" in first ? "Actualización de RTU" : first && typeof first === "object" && "TIEMPO" in first ? "Autorización de facturas" : "Estructura no reconocida";
            description.textContent = `${type} · ${Array.isArray(data) ? data.length : 0} registros · ${(file.size / 1024).toFixed(1)} KB`;
            const example = document.createElement("pre"); example.textContent = JSON.stringify(Array.isArray(data) ? data.slice(0, 2) : data, null, 2); box.append(example);
        } catch { description.textContent = "JSON malformado. La API devolverá el detalle del error al procesarlo."; }
    }
});
const upload = document.getElementById("upload-form");
if (upload) upload.addEventListener("submit", () => {
    const button = upload.querySelector("button[type=submit]"); button.disabled = true; button.textContent = "Procesando…";
});
