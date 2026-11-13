const fs = require('fs');
const path = require('path');
const files = {
    'jquery/dist/jquery.min.js': 'jquery/dist/jquery.min.js',
    'jquery-validation/dist/jquery.validate.min.js': 'jquery-validation/dist/jquery.validate.min.js',
    'jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js': 'jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js',
    'bootstrap/dist/js/bootstrap.bundle.min.js': 'bootstrap/dist/js/bootstrap.bundle.min.js'
};
for (const [source, target] of Object.entries(files)) {
    const input = path.join(__dirname, 'vendor/node_modules', source);
    const output = path.join(__dirname, '../wwwroot/lib', target);
    fs.mkdirSync(path.dirname(output), { recursive: true });
    fs.copyFileSync(input, output);
}
console.log('Bundled JavaScript refreshed from locked npm dependencies. Retain package license notices.');
