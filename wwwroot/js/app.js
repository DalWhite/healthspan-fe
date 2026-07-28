// Download a file from a .NET stream reference
window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName ?? '';
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
};

window.healthspanDocuments = {
    createObjectUrl: (content, contentType) => {
        const blob = new Blob([content], {
            type: contentType || 'application/octet-stream'
        });
        return URL.createObjectURL(blob);
    },
    revokeObjectUrl: (url) => {
        if (url) URL.revokeObjectURL(url);
    }
};

// Scroll to an element by ID and briefly highlight it
window.scrollToElement = function (elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;
    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    const row = el.closest('tr') ?? el.closest('li');
    if (row) {
        row.style.backgroundColor = 'rgba(25, 118, 210, 0.1)';
        setTimeout(() => row.style.backgroundColor = '', 2000);
    }
};
