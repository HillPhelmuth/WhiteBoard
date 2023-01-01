export function saveAsFile(filename, bytesBase64) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = `data:application/octet-stream;base64,${bytesBase64}`;
    document.body.appendChild(link); // Needed for Firefox
    link.click();
    document.body.removeChild(link);
}
export function getContainerLocation(containerId) {
    const e = document.querySelector(`[_bl_${containerId}=\"\"]`);
    const bnd = e.getBoundingClientRect();
    const out = { 'X': bnd.x, 'Y': bnd.y };
    console.log(JSON.stringify(out));
    return out;
}
export function getWidowDimentions() {
    const output = window.innerWidth;
    console.log(`window width ${output}`);
    return output;
}
export function getCanvasSize(containerId) {
    const e = document.querySelector(`[_bl_${containerId}=\"\"]`);
    const bnd = e.getBoundingClientRect();
    const out = { 'H': bnd.height, 'W': bnd.width };
    console.log(JSON.stringify(out));
    return out;
}