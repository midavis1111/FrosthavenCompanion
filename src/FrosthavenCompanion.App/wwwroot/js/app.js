// Returns [width, height] in CSS pixels of an element (for pan math).
window.frosthavenElementSize = (el) => el ? [el.getBoundingClientRect().width, el.getBoundingClientRect().height] : [0, 0];

// Triggers a browser download of the given text content as a file.
// Used by the campaign export feature.
window.frosthavenDownloadFile = (fileName, contentType, content) => {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
};
