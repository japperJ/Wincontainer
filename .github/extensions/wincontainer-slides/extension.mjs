// Extension: wincontainer-slides
// Interactive slide presentation about Wincontainer app
//
// Serves a self-contained HTML slide deck on an ephemeral local port.
// Use arrow keys or click/tap to navigate slides.

import { createServer } from "node:http";
import { joinSession, createCanvas } from "@github/copilot-sdk/extension";

const servers = new Map();
const slideState = new Map(); // instanceId -> { currentSlide }

function renderHtml(instanceId) {
  const stateKey = instanceId;
  if (!slideState.has(stateKey)) slideState.set(stateKey, { currentSlide: 0 });
  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8"/>
<meta name="viewport" content="width=device-width,initial-scale=1"/>
<title>Wincontainer — Slide Deck</title>
<style>
  @import url('https://fonts.googleapis.com/css2?family=Inter:opsz,wght@14..32,300..700&display=swap');
  *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
  html, body { height: 100%; overflow: hidden; font-family: 'Inter', system-ui, sans-serif; background: #0d1117; color: #e6edf3; }
  .deck { display: flex; height: 100vh; transition: transform .45s cubic-bezier(.22,1,.36,1); will-change: transform; }
  .slide { flex: 0 0 100%; display: flex; flex-direction: column; justify-content: center; align-items: center; padding: 3rem 4rem; position: relative; overflow-y: auto; }
  .slide-inner { max-width: 820px; width: 100%; }
  h1 { font-size: 3rem; font-weight: 700; letter-spacing: -.025em; line-height: 1.15; }
  h2 { font-size: 2rem; font-weight: 600; letter-spacing: -.015em; margin-bottom: 1.25rem; }
  h3 { font-size: 1.25rem; font-weight: 600; margin-bottom: .5rem; }
  p, li { font-size: 1.125rem; line-height: 1.65; color: #8b949e; }
  ul { list-style: none; padding: 0; }
  li { padding: .3rem 0; padding-left: 1.5rem; position: relative; }
  li::before { content: "▸"; position: absolute; left: 0; color: #58a6ff; }
  .tag { display: inline-block; background: #21262d; color: #c9d1d9; font-size: .8rem; font-weight: 600; padding: .2rem .65rem; border-radius: 6px; border: 1px solid #30363d; }
  .tag.blue { background: #0c2d6b; color: #58a6ff; border-color: #1f6feb; }
  .tag.green { background: #0a3a1f; color: #3fb950; border-color: #238636; }
  .tag.purple { background: #2a1454; color: #bc8cff; border-color: #6e40c9; }
  .tag.orange { background: #3d1f00; color: #d29922; border-color: #9e6a03; }
  .tags { display: flex; gap: .5rem; flex-wrap: wrap; margin-top: .75rem; }
  .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; }
  .grid-3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 1.25rem; }
  .card { background: #161b22; border: 1px solid #30363d; border-radius: 10px; padding: 1.25rem; }
  .card h3 { color: #f0f6fc; }
  .hero { text-align: center; }
  .hero h1 { font-size: 3.5rem; background: linear-gradient(135deg, #58a6ff, #bc8cff); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; }
  .hero .subtitle { font-size: 1.35rem; color: #8b949e; margin-top: .75rem; }
  .logo { font-size: 2rem; font-weight: 800; color: #58a6ff; margin-bottom: 1rem; display: flex; align-items: center; justify-content: center; gap: .5rem; }
  .nav { position: fixed; bottom: 2rem; left: 50%; transform: translateX(-50%); display: flex; align-items: center; gap: 1rem; background: #161b22; border: 1px solid #30363d; border-radius: 999px; padding: .5rem 1rem; z-index: 100; }
  .nav button { background: none; border: none; color: #8b949e; font-size: 1.25rem; cursor: pointer; padding: .25rem .5rem; border-radius: 6px; transition: .15s; }
  .nav button:hover { color: #f0f6fc; background: #21262d; }
  #autoPlayBtn { font-size: .85rem; padding: .35rem .75rem; background: #1f6feb; color: #fff; border-radius: 999px; font-weight: 600; }
  #autoPlayBtn:hover { background: #388bfd; color: #fff; }
  #autoPlayBtn.playing { background: #da3633; }
  #autoPlayBtn.playing:hover { background: #f85149; }
  #loopBtn { font-size: 1.1rem; opacity: .4; transition: .2s; }
  #loopBtn.active { opacity: 1; color: #3fb950; }
  .nav .dots { display: flex; gap: .35rem; }
  .nav .dot { width: 8px; height: 8px; border-radius: 50%; background: #30363d; transition: .2s; cursor: pointer; }
  .nav .dot.active { background: #58a6ff; width: 22px; border-radius: 4px; }
  .progress { position: fixed; top: 0; left: 0; height: 3px; background: #58a6ff; transition: width .4s ease; z-index: 200; }
  .slide-number { position: fixed; top: 1rem; right: 1.5rem; font-size: .8rem; color: #484f58; font-variant-numeric: tabular-nums; z-index: 100; }
  .feature-icon { font-size: 2rem; margin-bottom: .5rem; }
  .arch-flow { display: flex; align-items: center; justify-content: center; gap: 1rem; margin: 1.5rem 0; flex-wrap: wrap; }
  .arch-box { background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: .75rem 1.25rem; font-size: .9rem; font-weight: 600; text-align: center; }
  .arch-arrow { color: #484f58; font-size: 1.25rem; }
  .code { background: #21262d; border: 1px solid #30363d; border-radius: 6px; padding: .4rem .7rem; font-family: 'SF Mono', 'Cascadia Code', monospace; font-size: .85rem; color: #c9d1d9; display: inline-block; }
  .req-grid { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem; }
  .req-item { display: flex; align-items: center; gap: .6rem; background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: .75rem 1rem; }
  .req-item .check { color: #3fb950; font-size: 1.1rem; }
  a { color: #58a6ff; text-decoration: none; }
  a:hover { text-decoration: underline; }
  .footer-text { font-size: .85rem; color: #484f58; margin-top: 2rem; }
  @media (max-width: 640px) { .slide { padding: 1.5rem; } h1 { font-size: 2rem; } h2 { font-size: 1.5rem; } .grid-2, .grid-3 { grid-template-columns: 1fr; } .hero h1 { font-size: 2.25rem; } }
</style>
</head>
<body>
<div class="progress" id="progress"></div>
<div class="slide-number" id="slideNum"></div>
<div class="deck" id="deck">

<!-- SLIDE 1: Title -->
<div class="slide"><div class="slide-inner hero">
  <div class="logo">⬡ Wincontainer</div>
  <h1>A Native Windows Container Manager</h1>
  <p class="subtitle">Manage WSL Containers (WSLC) with a sleek WinUI 3 desktop app — no Docker Desktop required.</p>
  <div class="tags" style="justify-content:center;margin-top:1.25rem">
    <span class="tag blue">WinUI 3</span>
    <span class="tag green">Windows App SDK</span>
    <span class="tag purple">WSLC Runtime</span>
    <span class="tag orange">Open Source</span>
  </div>
  <p class="footer-text">← Use arrow keys or dots to navigate →</p>
</div></div>

<!-- SLIDE 2: What is Wincontainer -->
<div class="slide"><div class="slide-inner">
  <h2>What is Wincontainer?</h2>
  <p style="margin-bottom:1.25rem">A focused Windows desktop manager for containers running through Microsoft's WSL Containers runtime (WSLC). It gives developers a small, native interface for viewing and operating containers without Docker Desktop.</p>
  <div class="grid-2">
    <div class="card">
      <div class="feature-icon">🎯</div>
      <h3>Focused Scope</h3>
      <p>A lightweight utility, not a platform — does exactly what container management needs and nothing else.</p>
    </div>
    <div class="card">
      <div class="feature-icon">⚡</div>
      <h3>Native Performance</h3>
      <p>Built with WinUI 3 and the Windows App SDK for native Windows 11 look, feel, and performance.</p>
    </div>
    <div class="card">
      <div class="feature-icon">🔌</div>
      <h3>No Docker Dependency</h3>
      <p>Uses wslc.exe directly — no Docker Desktop, no extra daemons, no licensing overhead.</p>
    </div>
    <div class="card">
      <div class="feature-icon">📦</div>
      <h3>Portable or Installed</h3>
      <p>Run as a portable app from any folder, or install with the Windows setup executable.</p>
    </div>
  </div>
</div></div>

<!-- SLIDE 3: Key Features -->
<div class="slide"><div class="slide-inner">
  <h2>Key Features</h2>
  <div class="grid-3">
    <div class="card"><div class="feature-icon">▶️</div><h3>Container Lifecycle</h3><p>View, start, stop, restart, and remove containers with one click.</p></div>
    <div class="card"><div class="feature-icon">📥</div><h3>Image Management</h3><p>Pull, inspect, and remove container images from your registry.</p></div>
    <div class="card"><div class="feature-icon">🔷</div><h3>Volumes & Networks</h3><p>Manage storage volumes and container networks through the UI.</p></div>
    <div class="card"><div class="feature-icon">📋</div><h3>Container Logs</h3><p>Open real-time container logs inline — no need for CLI grep.</p></div>
    <div class="card"><div class="feature-icon">💻</div><h3>Interactive Terminal</h3><p>Launch an in-app terminal into any running container.</p></div>
    <div class="card"><div class="feature-icon">🔍</div><h3>Onboarding Wizard</h3><p>Detect WSL2, virtualization, and WSLC prerequisites with guided setup.</p></div>
    <div class="card"><div class="feature-icon">🔄</div><h3>Auto-Updates</h3><p>Check for Stable or Beta application updates from GitHub Releases.</p></div>
    <div class="card"><div class="feature-icon">🖥️</div><h3>System Tray</h3><p>Background tray presence with quick-access to container status.</p></div>
    <div class="card"><div class="feature-icon">🐳</div><h3>Compose Support</h3><p>Manage Docker Compose projects directly from the UI.</p></div>
  </div>
</div></div>

<!-- SLIDE 4: Architecture -->
<div class="slide"><div class="slide-inner">
  <h2>Architecture</h2>
  <div class="card" style="margin-bottom:1rem">
    <h3>Process Model</h3>
    <div class="arch-flow">
      <div class="arch-box">WinContainers.App<br><span style="font-weight:400;font-size:.8rem">WinUI UI + Kestrel API</span></div>
      <div class="arch-arrow">⇄</div>
      <div class="arch-box">Service Host<br><span style="font-weight:400;font-size:.8rem">In-process REST API</span></div>
      <div class="arch-arrow">⇄</div>
      <div class="arch-box">WslcDriver<br><span style="font-weight:400;font-size:.8rem">wslc.exe calls</span></div>
      <div class="arch-arrow">→</div>
      <div class="arch-box">WSLC Runtime<br><span style="font-weight:400;font-size:.8rem">WSL Containers</span></div>
    </div>
    <p style="font-size:.95rem">Everything runs in a single process — the WinUI window, the Kestrel API server, and the ServiceHost all coexist in-process.</p>
  </div>
  <div class="grid-2">
    <div class="card">
      <h3>WinContainers.Core</h3>
      <p style="font-size:.95rem">Shared commands, models, and value types used across the solution.</p>
    </div>
    <div class="card">
      <h3>WinContainers.Runtime</h3>
      <p style="font-size:.95rem">WSLC execution, output parsing, and runtime models. Owns the WslcDriver.</p>
    </div>
    <div class="card">
      <h3>WinContainers.Service</h3>
      <p style="font-size:.95rem">REST endpoint definitions hosted by ServiceHost inside the app process.</p>
    </div>
    <div class="card">
      <h3>BuildTasks</h3>
      <p style="font-size:.95rem">Custom MSBuild task (FixCulture) used during the app build pipeline.</p>
    </div>
  </div>
</div></div>

<!-- SLIDE 5: Project Structure -->
<div class="slide"><div class="slide-inner">
  <h2>Project Layout</h2>
  <div class="grid-2">
    <div class="card">
      <h3>📁 src/</h3>
      <ul style="font-size:.95rem">
        <li>WinContainers.App — WinUI 3 app, pages, VMs</li>
        <li>WinContainers.Core — Shared models & commands</li>
        <li>WinContainers.Runtime — wslc driver & parsers</li>
        <li>WinContainers.Service — REST API endpoints</li>
        <li>BuildTasks — MSBuild fix-culture task</li>
      </ul>
    </div>
    <div class="card">
      <h3>📁 tests/</h3>
      <ul style="font-size:.95rem">
        <li>Unit — Core & Runtime unit tests</li>
        <li>Integration — Full-stack integration tests</li>
        <li>Playwright — UI automation tests</li>
        <li>Ui — Additional UI test suite</li>
      </ul>
    </div>
    <div class="card">
      <h3>📁 tools/</h3>
      <ul style="font-size:.95rem">
        <li>build-release.ps1 — Release packaging</li>
        <li>generate-cert.ps1 — Local signing cert</li>
      </ul>
    </div>
    <div class="card">
      <h3>📄 Key Files</h3>
      <ul style="font-size:.95rem">
        <li>WinContainers.slnx — Solution file</li>
        <li>Directory.Build.props — Build config</li>
        <li>Directory.Packages.props — Package versions</li>
      </ul>
    </div>
  </div>
</div></div>

<!-- SLIDE 6: Pages & Views -->
<div class="slide"><div class="slide-inner">
  <h2>UI Pages & Controls</h2>
  <div class="grid-2">
    <div class="card"><span class="tag" style="margin-bottom:.5rem">Dashboard</span><h3>DashboardPage</h3><p>Main overview with container status, quick actions, and resource usage at a glance.</p></div>
    <div class="card"><span class="tag blue" style="margin-bottom:.5rem">Containers</span><h3>ContainersControl</h3><p>Full container list with start/stop/restart/remove actions and status indicators.</p></div>
    <div class="card"><span class="tag green" style="margin-bottom:.5rem">Details</span><h3>ContainerDetailPage</h3><p>Deep inspection of a single container — logs, env vars, mounts, and port mappings.</p></div>
    <div class="card"><span class="tag purple" style="margin-bottom:.5rem">Images</span><h3>ImagesPage</h3><p>Browse, pull, inspect, and remove container images.</p></div>
    <div class="card"><span class="tag orange" style="margin-bottom:.5rem">Terminal</span><h3>TerminalPage</h3><p>In-app interactive shell into any running container.</p></div>
    <div class="card"><span class="tag" style="margin-bottom:.5rem">Settings</span><h3>SettingsPage</h3><p>Application settings, update channel, and WSLC configuration.</p></div>
    <div class="card"><span class="tag blue" style="margin-bottom:.5rem">Resources</span><h3>ResourcesControl</h3><p>Manage volumes and networks.</p></div>
    <div class="card"><span class="tag green" style="margin-bottom:.5rem">Onboarding</span><h3>OnboardingPage</h3><p>First-run wizard that checks prerequisites and guides WSLC setup.</p></div>
    <div class="card"><span class="tag purple" style="margin-bottom:.5rem">Compose</span><h3>ComposeControl</h3><p>Manage Docker Compose projects from the UI.</p></div>
    <div class="card"><span class="tag orange" style="margin-bottom:.5rem">Templates</span><h3>TemplateCatalogControl</h3><p>Browse and launch container templates.</p></div>
  </div>
</div></div>

<!-- SLIDE 7: Technology Stack -->
<div class="slide"><div class="slide-inner">
  <h2>Technology Stack</h2>
  <div class="grid-2">
    <div class="card"><h3>🖥️ Frontend</h3><ul><li>WinUI 3 (Windows App SDK 1.8+)</li><li>XAML with x:Bind + x:DataType</li><li>MVVM with ViewModelLocator</li><li>ObservableCollection for live lists</li><li>DataTemplates with code-behind Click</li></ul></div>
    <div class="card"><h3>⚙️ Backend</h3><ul><li>C# / .NET 10 (net10.0)</li><li>ASP.NET Core Kestrel (in-process)</li><li>WslcDriver for wslc.exe interaction</li><li>MSBuild custom tasks</li><li>REST API via ServiceHost</li></ul></div>
    <div class="card"><h3>🧪 Testing</h3><ul><li>Unit tests (xUnit)</li><li>Integration tests</li><li>Playwright UI tests</li><li>Full solution build validation</li></ul></div>
    <div class="card"><h3>📦 Packaging</h3><ul><li>PowerShell release script</li><li>Windows setup executable</li><li>Portable ZIP distribution</li><li>GitHub Releases publishing</li></ul></div>
  </div>
</div></div>

<!-- SLIDE 8: Requirements -->
<div class="slide"><div class="slide-inner">
  <h2>System Requirements</h2>
  <div class="req-grid">
    <div class="req-item"><span class="check">✓</span> Windows 11</div>
    <div class="req-item"><span class="check">✓</span> WSL2 enabled</div>
    <div class="req-item"><span class="check">✓</span> Virtualization enabled (BIOS/EFI)</div>
    <div class="req-item"><span class="check">✓</span> wslc.exe on PATH</div>
    <div class="req-item"><span class="check">✓</span> Admin approval for WSL2/WSLC install</div>
    <div class="req-item"><span class="check">✓</span> .NET 10 SDK (to build from source)</div>
  </div>
  <div class="card" style="margin-top:1rem">
    <h3>Build Commands</h3>
    <p><span class="code">dotnet build WinContainers.slnx -c Debug --nologo -v q</span></p>
    <p style="margin-top:.5rem"><span class="code">dotnet publish src/WinContainers.App -c Debug -r win-x64 -o publish/WinContainers</span></p>
  </div>
</div></div>

<!-- SLIDE 9: WSL Container Runtime -->
<div class="slide"><div class="slide-inner">
  <h2>WSLC Runtime</h2>
  <div class="card" style="margin-bottom:1rem">
    <p>Wincontainer uses <span class="code">wslc.exe</span> as its container runtime. It does <strong>not</strong> bundle Docker Desktop or use Docker Desktop binaries. All container commands run through the local WSLC runtime via the <span class="code">WslcDriver</span> class.</p>
  </div>
  <div class="grid-2">
    <div class="card">
      <h3>Key Runtime Classes</h3>
      <ul style="font-size:.95rem">
        <li>WslcDriver — Main wslc.exe driver</li>
        <li>WslcContainerParser — Container output parser</li>
        <li>WslcResourceParser — Volume/network parser</li>
        <li>WslcFileParser — File operation parser</li>
        <li>RuntimeTools — Runtime utility helpers</li>
      </ul>
    </div>
    <div class="card">
      <h3>WSLC Commands</h3>
      <ul style="font-size:.95rem">
        <li>WslcCommands — Command definitions</li>
        <li>WslcVersionFormatter — Version display</li>
        <li>Polling every 10 seconds for container status</li>
        <li>Keep-alive WSL process management</li>
      </ul>
    </div>
  </div>
</div></div>

<!-- SLIDE 10: Open Source & Credits -->
<div class="slide"><div class="slide-inner hero">
  <h2>Open Source</h2>
  <p style="margin-bottom:.75rem">Wincontainer is free and open source under the <strong>MIT License</strong>.</p>
  <p>It is a focused local desktop utility — not a hosted service or paid SaaS product.</p>
  <div class="card" style="margin-top:1.5rem;text-align:left">
    <h3>📂 GitHub</h3>
    <p><a href="https://github.com/japperJ/Wincontainer" target="_blank">github.com/japperJ/Wincontainer</a></p>
    <p style="margin-top:.75rem;font-size:.95rem">Download the latest installer or portable ZIP from the Releases page.</p>
  </div>
  <div class="card" style="margin-top:.75rem;text-align:left">
    <h3>👤 Author</h3>
    <p>Created by <a href="https://github.com/japperj" target="_blank">Jan Petersen</a></p>
  </div>
  <p class="footer-text" style="margin-top:1.5rem">⬡ Wincontainer ${new Date().getFullYear()} — MIT Licensed</p>
</div></div>

</div>

<div class="nav" id="nav">
  <button id="prevBtn" title="Previous">◀</button>
  <div class="dots" id="dots"></div>
  <button id="nextBtn" title="Next">▶</button>
  <button id="autoPlayBtn" title="Auto-play all slides">▶ Auto-play</button>
  <button id="loopBtn" title="Loop auto-play">🔁</button>
</div>

<script>
  (function() {
    const deck = document.getElementById('deck');
    const slides = deck.children;
    const total = slides.length;
    let current = 0;

    const progress = document.getElementById('progress');
    const slideNum = document.getElementById('slideNum');
    const dotsContainer = document.getElementById('dots');
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');

    for (let i = 0; i < total; i++) {
      const dot = document.createElement('div');
      dot.className = 'dot';
      dot.addEventListener('click', () => goTo(i));
      dotsContainer.appendChild(dot);
    }

    function goTo(index) {
      if (index < 0) index = 0;
      if (index >= total) index = total - 1;
      if (index === current) return;
      current = index;
      deck.style.transform = 'translateX(-' + (current * 100) + '%)';
      progress.style.width = ((current + 1) / total * 100) + '%';
      slideNum.textContent = (current + 1) + ' / ' + total;
      const dots = dotsContainer.children;
      for (let i = 0; i < total; i++) dots[i].classList.toggle('active', i === current);
    }

    // Poll server for remote slide changes
    async function pollSlide() {
      try {
        const res = await fetch('/current-slide');
        const data = await res.json();
        if (typeof data.slide === 'number' && data.slide !== current) {
          goTo(data.slide);
        }
      } catch(e) {}
      setTimeout(pollSlide, 500);
    }
    pollSlide();

    let autoPlayTimer = null;
    let loopEnabled = false;
    const autoPlayBtn = document.getElementById('autoPlayBtn');
    const loopBtn = document.getElementById('loopBtn');

    loopBtn.addEventListener('click', () => {
      loopEnabled = !loopEnabled;
      loopBtn.classList.toggle('active', loopEnabled);
      loopBtn.title = loopEnabled ? 'Looping on' : 'Looping off';
    });

    function stopAutoPlay() {
      if (autoPlayTimer) {
        clearTimeout(autoPlayTimer);
        autoPlayTimer = null;
      }
      autoPlayBtn.textContent = '▶ Auto-play';
      autoPlayBtn.classList.remove('playing');
    }

    function startAutoPlay() {
      stopAutoPlay();
      autoPlayBtn.textContent = '⏹ Stop';
      autoPlayBtn.classList.add('playing');

      function advance() {
        if (current >= total - 1) {
          if (loopEnabled) {
            goTo(0);
            fetch('/set-slide/0');
            autoPlayTimer = setTimeout(advance, 10000);
          } else {
            stopAutoPlay();
            fetch('/set-slide/' + (total - 1));
          }
          return;
        }
        goTo(current + 1);
        fetch('/set-slide/' + (current));
        autoPlayTimer = setTimeout(advance, 10000);
      }
      autoPlayTimer = setTimeout(advance, 10000);
    }

    autoPlayBtn.addEventListener('click', () => {
      if (autoPlayTimer) {
        stopAutoPlay();
      } else {
        startAutoPlay();
      }
    });

    prevBtn.addEventListener('click', () => { stopAutoPlay(); goTo(current - 1); fetch('/set-slide/' + (current)); });
    nextBtn.addEventListener('click', () => { stopAutoPlay(); goTo(current + 1); fetch('/set-slide/' + (current)); });
    document.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') { stopAutoPlay(); goTo(current - 1); fetch('/set-slide/' + (current)); }
      if (e.key === 'ArrowRight' || e.key === 'ArrowDown' || e.key === ' ') { e.preventDefault(); stopAutoPlay(); goTo(current + 1); fetch('/set-slide/' + (current)); }
    });
    document.addEventListener('touchstart', (e) => {
      const x = e.touches[0].clientX;
      const handler = (ev) => {
        const dx = ev.changedTouches[0].clientX - x;
        if (Math.abs(dx) > 50) { stopAutoPlay(); goTo(current + (dx < 0 ? 1 : -1)); fetch('/set-slide/' + (current)); }
        document.removeEventListener('touchend', handler);
      };
      document.addEventListener('touchend', handler);
    });

    // Stop auto-play if dot clicked
    dotsContainer.addEventListener('click', (e) => {
      if (e.target.classList.contains('dot')) stopAutoPlay();
    });

    goTo(0);
  })();
</script>
</body>
</html>`;
}

async function startServer(instanceId) {
    const stateKey = instanceId;
    if (!slideState.has(stateKey)) slideState.set(stateKey, { currentSlide: 0 });

    const server = createServer((req, res) => {
        const url = new URL(req.url, `http://${req.headers.host}`);
        const path = url.pathname;

        if (path === "/current-slide") {
            res.setHeader("Content-Type", "application/json");
            res.end(JSON.stringify({ slide: slideState.get(stateKey)?.currentSlide ?? 0 }));
            return;
        }

        const setSlideMatch = path.match(/^\/set-slide\/(\d+)$/);
        if (setSlideMatch) {
            const slide = parseInt(setSlideMatch[1], 10);
            const state = slideState.get(stateKey);
            if (state) state.currentSlide = slide;
            res.setHeader("Content-Type", "application/json");
            res.end(JSON.stringify({ ok: true, slide }));
            return;
        }

        res.setHeader("Content-Type", "text/html; charset=utf-8");
        res.end(renderHtml(instanceId));
    });
    await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
    const address = server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    return { server, port, url: `http://127.0.0.1:${port}/` };
}

const session = await joinSession({
    canvases: [
        createCanvas({
            id: "wincontainer-slides",
            displayName: "Wincontainer Slides",
            description: "Interactive slide deck about the Wincontainer app — architecture, features, and technology stack.",
            actions: [
                {
                    name: "go_to_slide",
                    description: "Navigate to a specific slide by number (1-based).",
                    inputSchema: {
                        type: "object",
                        properties: {
                            slide: { type: "number", description: "Slide number to navigate to (1-10)" },
                        },
                        required: ["slide"],
                    },
                    handler: async (ctx) => {
                        const slide = ctx.input?.slide;
                        if (typeof slide === "number") {
                            const state = slideState.get(ctx.instanceId);
                            if (state) state.currentSlide = Math.max(0, Math.min(9, slide - 1));
                            const entry = servers.get(ctx.instanceId);
                            if (entry) {
                                try {
                                    const fetch = (await import("node:http")).get;
                                    await new Promise((resolve, reject) => {
                                        const req = fetch("http://127.0.0.1:" + entry.port + "/set-slide/" + (state ? state.currentSlide : 0), (res) => { res.resume(); res.on("end", resolve); });
                                        req.on("error", reject);
                                        req.end();
                                    });
                                } catch {}
                            }
                        }
                        return { ok: true, instanceId: ctx.instanceId, slide: slide };
                    },
                },
            ],
            open: async (ctx) => {
                let entry = servers.get(ctx.instanceId);
                if (!entry) {
                    entry = await startServer(ctx.instanceId);
                    servers.set(ctx.instanceId, entry);
                }
                return {
                    title: "Wincontainer — Slide Deck",
                    url: entry.url,
                };
            },
            onClose: async (ctx) => {
                const entry = servers.get(ctx.instanceId);
                if (entry) {
                    servers.delete(ctx.instanceId);
                    await new Promise((resolve) => entry.server.close(() => resolve()));
                }
            },
        }),
    ],
});
