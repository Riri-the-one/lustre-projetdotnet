const fs = require('fs');
const { execFileSync } = require('child_process');
const files = execFileSync('git', ['ls-files', '--cached', '--others', '--exclude-standard'], { encoding: 'utf8' }).trim().split('\n');
const patterns = [/AKIA[0-9A-Z]{16}/, /-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/, /Admin@123/, /admin@gmail\.com/];
const findings = [];
for (const file of files) {
    if (!fs.existsSync(file) || !/\.(cs|cshtml|json|ya?ml|md|config|user|ps1|cjs|env)$/.test(file) || file === 'scripts/check-secrets.cjs') continue;
    const content = fs.readFileSync(file, 'utf8');
    if (patterns.some(pattern => pattern.test(content))) findings.push(file);
}
console.log(JSON.stringify({ filesConsidered: files.length, matchedFiles: findings }, null, 2));
if (findings.length) process.exitCode = 1;
