const fs = require('fs');
const path = require('path');
(async () => {
    const index = await (await fetch('https://api.nuget.org/v3/vulnerabilities/index.json')).json();
    const sources = await Promise.all(index.map(async x => ({ url: x['@id'], data: await (await fetch(x['@id'])).json() })));
    const compare = (a, b) => {
        const x = a.split(/[.-]/).map(Number), y = b.split(/[.-]/).map(Number);
        for (let i = 0; i < Math.max(x.length, y.length); i++) { const d = (x[i] || 0) - (y[i] || 0); if (d) return Math.sign(d); }
        return 0;
    };
    function matches(version, range) {
        if (version.includes('-')) throw Error('Prerelease version requires NuGet-native audit: ' + version);
        const ends = range.slice(1, -1).split(',').map(s => s.trim());
        if (ends.length === 1) return compare(version, ends[0]) === 0;
        return (!ends[0] || compare(version, ends[0]) > 0 || (range[0] === '[' && compare(version, ends[0]) === 0))
            && (!ends[1] || compare(version, ends[1]) < 0 || (range.at(-1) === ']' && compare(version, ends[1]) === 0));
    }
    const packages = new Set();
    for (const file of ['obj/project.assets.json', 'tests/Lustre.Tests/obj/project.assets.json']) {
        const assets = JSON.parse(fs.readFileSync(file, 'utf8'));
        for (const [name, lib] of Object.entries(assets.libraries)) if (lib.type === 'package') packages.add(name);
    }
    const findings = [];
    for (const entry of packages) {
        const [name, version] = entry.split('/');
        for (const source of sources) for (const advisory of source.data[name.toLowerCase()] || [])
            if (matches(version, advisory.versions)) findings.push({ name, version, ...advisory });
    }
    fs.mkdirSync('.artifacts', { recursive: true });
    const report = { checkedAt: new Date().toISOString(), packagesChecked: packages.size,
        sources: sources.map(s => s.url), findings: [...new Map(findings.map(f => [f.name + f.url, f])).values()] };
    fs.writeFileSync('.artifacts/dependency-audit.json', JSON.stringify(report, null, 2));
    console.log(JSON.stringify(report, null, 2));
    if (findings.length) process.exitCode = 1;
})().catch(e => { console.error(e); process.exitCode = 1; });
