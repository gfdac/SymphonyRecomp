// ==========================================================================
// SymphonyRecomp - Interactive App Logic & Multiplayer Radar Simulator
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
    initRadarSimulator();
    initShortcutSearch();
    initNavbarScroll();
});

// 1. Interactive Co-op Radar Simulator (Canvas)
function initRadarSimulator() {
    const canvas = document.getElementById('radarCanvas');
    if (!canvas) return;
    const ctx = canvas.getContext('2d');

    const cols = 48;
    const rows = 24;
    const tileW = canvas.width / cols;
    const tileH = canvas.height / rows;

    // Grid states: 0 = unvisited, 1 = local (blue), 2 = remote (orange), 3 = both (green)
    const mapGrid = Array(rows).fill(0).map(() => Array(cols).fill(0));

    // Seed some initial visited rooms
    for (let r = 8; r < 16; r++) {
        for (let c = 10; c < 22; c++) {
            mapGrid[r][c] = (c < 18) ? 1 : 3;
        }
    }
    for (let r = 10; r < 18; r++) {
        for (let c = 22; c < 34; c++) {
            mapGrid[r][c] = 2;
        }
    }

    let localPos = { x: 14, y: 12 };
    let remotePos = { x: 30, y: 14 };
    let warpAnim = 0;

    function draw() {
        ctx.fillStyle = '#08070d';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        // Draw Tiles
        for (let r = 0; r < rows; r++) {
            for (let c = 0; c < cols; c++) {
                const state = mapGrid[r][c];
                if (state === 0) continue;

                if (state === 1) ctx.fillStyle = 'rgba(56, 189, 248, 0.75)'; // Blue (Local)
                else if (state === 2) ctx.fillStyle = 'rgba(249, 115, 22, 0.75)'; // Orange (Remote)
                else if (state === 3) ctx.fillStyle = 'rgba(34, 197, 94, 0.85)'; // Green (Both)

                ctx.fillRect(c * tileW, r * tileH, tileW - 1, tileH - 1);
            }
        }

        // Draw Local Player Dot (White / Cyan pulse)
        ctx.fillStyle = '#ffffff';
        ctx.beginPath();
        ctx.arc(localPos.x * tileW + tileW / 2, localPos.y * tileH + tileH / 2, 4.5, 0, Math.PI * 2);
        ctx.fill();

        ctx.strokeStyle = '#38bdf8';
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.arc(localPos.x * tileW + tileW / 2, localPos.y * tileH + tileH / 2, 6.5, 0, Math.PI * 2);
        ctx.stroke();

        // Draw Remote Player Triangle (Red / Gold)
        const rx = remotePos.x * tileW + tileW / 2;
        const ry = remotePos.y * tileH + tileH / 2;

        ctx.fillStyle = '#ef4444';
        ctx.beginPath();
        ctx.moveTo(rx, ry - 6);
        ctx.lineTo(rx - 5, ry + 5);
        ctx.lineTo(rx + 5, ry + 5);
        ctx.closePath();
        ctx.fill();

        ctx.fillStyle = '#f5b942';
        ctx.font = '10px Outfit, sans-serif';
        ctx.fillText('Partner', rx + 8, ry + 2);

        // Warp Ripple Effect
        if (warpAnim > 0) {
            ctx.strokeStyle = `rgba(245, 185, 66, ${warpAnim / 20})`;
            ctx.lineWidth = 3;
            ctx.beginPath();
            ctx.arc(localPos.x * tileW + tileW / 2, localPos.y * tileH + tileH / 2, (20 - warpAnim) * 3, 0, Math.PI * 2);
            ctx.stroke();
            warpAnim--;
        }

        requestAnimationFrame(draw);
    }

    draw();

    // Button: Simulate Move
    const btnMove = document.getElementById('btnSimulateMove');
    if (btnMove) {
        btnMove.addEventListener('click', () => {
            const dx = Math.floor(Math.random() * 3) - 1;
            const dy = Math.floor(Math.random() * 3) - 1;
            remotePos.x = Math.max(2, Math.min(cols - 3, remotePos.x + dx));
            remotePos.y = Math.max(2, Math.min(rows - 3, remotePos.y + dy));

            // Mark tile
            const cur = mapGrid[remotePos.y][remotePos.x];
            if (cur === 1 || cur === 3) mapGrid[remotePos.y][remotePos.x] = 3;
            else mapGrid[remotePos.y][remotePos.x] = 2;
        });
    }

    // Button: Buddy Warp
    const btnWarp = document.getElementById('btnBuddyWarp');
    if (btnWarp) {
        btnWarp.addEventListener('click', () => {
            localPos.x = remotePos.x;
            localPos.y = remotePos.y;
            mapGrid[localPos.y][localPos.x] = 3;
            warpAnim = 20;
        });
    }
}

// 2. Shortcuts Table Search
function initShortcutSearch() {
    const input = document.getElementById('shortcutSearch');
    const table = document.getElementById('shortcutsTable');
    if (!input || !table) return;

    input.addEventListener('input', () => {
        const query = input.value.toLowerCase().trim();
        const rows = table.querySelectorAll('tbody tr');

        rows.forEach(row => {
            const text = row.innerText.toLowerCase();
            row.style.display = text.includes(query) ? '' : 'none';
        });
    });
}

// 3. Navbar Scroll Blur
function initNavbarScroll() {
    const navbar = document.getElementById('navbar');
    if (!navbar) return;

    window.addEventListener('scroll', () => {
        if (window.scrollY > 50) {
            navbar.style.background = 'rgba(10, 9, 15, 0.95)';
            navbar.style.boxShadow = '0 4px 20px rgba(0, 0, 0, 0.6)';
        } else {
            navbar.style.background = 'rgba(18, 15, 29, 0.75)';
            navbar.style.boxShadow = 'none';
        }
    });
}

// 4. Copy Code Helper
window.copyCode = function(button) {
    const codeBox = button.closest('.code-box');
    const code = codeBox.querySelector('code').innerText;

    navigator.clipboard.writeText(code).then(() => {
        const icon = button.querySelector('i');
        icon.className = 'fa-solid fa-check text-success';
        setTimeout(() => {
            icon.className = 'fa-regular fa-copy';
        }, 2000);
    });
};
