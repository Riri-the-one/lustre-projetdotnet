const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const assert = require('node:assert/strict');

(async () => {
    const baseURL = process.env.LUSTRE_BASE_URL || 'http://127.0.0.1:5087';
    const customerPassword = process.env.LUSTRE_CUSTOMER_PASSWORD;
    const adminPassword = process.env.LUSTRE_ADMIN_PASSWORD;
    assert(customerPassword && adminPassword, 'Set LUSTRE_CUSTOMER_PASSWORD and LUSTRE_ADMIN_PASSWORD for the isolated --demo instance.');
    const screenshots = path.resolve(__dirname, '../../docs/screenshots');
    const artifacts = path.resolve(__dirname, '../../.artifacts');
    fs.mkdirSync(screenshots, { recursive: true });
    fs.mkdirSync(artifacts, { recursive: true });
    const report = { browser: 'Microsoft Edge via Playwright', checks: [], javascriptErrors: [], resourceErrors: [], overflow: [], failedRequests: [] };
    const browser = await chromium.launch({ channel: process.env.LUSTRE_BROWSER_CHANNEL || 'msedge', headless: true });
    const context = await browser.newContext({ baseURL, viewport: { width: 1440, height: 1000 } });
    const page = await context.newPage();
    page.on('response', response => { if (response.url().startsWith(baseURL) && response.status() >= 400) report.resourceErrors.push({ url: response.url().split('?')[0], status: response.status() }); });
    page.on('pageerror', e => report.javascriptErrors.push(e.message));
    page.on('requestfailed', r => report.failedRequests.push({ url: r.url().split('?')[0], error: r.failure()?.errorText }));
    const check = name => { report.checks.push(name); console.log('PASS', name); };
    async function shot(name) { await page.screenshot({ path: path.join(screenshots, name + '.png'), fullPage: false }); }
    async function overflow(label) {
        const result = await page.evaluate(() => ({ width: innerWidth, documentWidth: document.documentElement.scrollWidth,
            elements: [...document.querySelectorAll('body *')].filter(e => e.getBoundingClientRect().right > innerWidth + 1).slice(0, 6).map(e => e.className) }));
        if (result.documentWidth > result.width + 1) report.overflow.push({ label, ...result });
    }
    async function login(email, password) {
        await page.goto('/Identity/Account/Login');
        await page.locator('#Input_Email').fill(email);
        await page.locator('#Input_Password').fill(password);
        await Promise.all([page.waitForURL(u => !u.pathname.includes('/Login')), page.locator('button[type="submit"] ').first().click()]);
    }
    try {
        await page.goto('/'); await shot('home-desktop'); check('Home renders');
        for (const width of [320, 390, 768, 1440]) {
            await page.setViewportSize({ width, height: 900 }); await overflow('home-' + width);
            if (width === 390) await shot('mobile');
        }
        await page.setViewportSize({ width: 1440, height: 1000 });
        await page.goto('/Home/Index?sterm=cleanser');
        assert.equal(await page.locator('#products .product-card').count(), 1);
        check('Case-insensitive catalogue search');
        await page.goto('/Home/Index?categoryId=1#products');
        await page.locator('#products').scrollIntoViewIfNeeded(); await shot('catalogue'); check('Category filter');
        await page.goto('/Identity/Account/Register');
        await page.locator('#Input_Email').fill('browser-' + Date.now() + '@example.test');
        await page.locator('#Input_Password').fill(customerPassword);
        await page.locator('#Input_ConfirmPassword').fill(customerPassword);
        await Promise.all([page.waitForURL(u => !u.pathname.includes('/Register')), page.locator('button[type="submit"] ').first().click()]);
        check('Real Identity registration');
        await context.clearCookies();
        await login('customer@lustre.example', customerPassword); check('Real Identity customer login');
        await page.goto('/');
        await Promise.all([page.waitForResponse(r => r.url().includes('/Cart/AddItem') && r.request().method() === 'POST'), page.locator('.add-to-cart-btn').first().click()]);
        await page.goto('/Cart/GetUserCart');
        assert(await page.locator('body').innerText().then(t => t.includes('Gentle Cleanser')));
        await shot('cart'); check('Cart add through CSRF-protected JavaScript');
        for (const width of [320, 390, 768, 1440]) { await page.setViewportSize({ width, height: 900 }); await overflow('cart-' + width); }
        await page.setViewportSize({ width: 1440, height: 1000 });
        await page.goto('/Cart/Checkout');
        await page.locator('#Name').fill('Demo Customer'); await page.locator('#Email').fill('customer@lustre.example');
        await page.locator('#MobileNumber').fill('0600000000'); await page.locator('#Address').fill('Fictitious address, Casablanca');
        await page.locator('#PaymentMethod').selectOption('COD'); await shot('checkout');
        await Promise.all([page.waitForURL('**/Cart/OrderSuccess'), page.locator('.checkout-submit-btn').click()]);
        check('Checkout completes with simulated payment');
        await page.goto('/UserOrder/UserOrders'); await shot('orders'); check('Customer order history');
        await page.goto('/AdminOperations/Dashboard');
        assert(page.url().includes('AccessDenied')); check('Customer denied administrator access');
        await context.clearCookies(); await login('admin@lustre.example', adminPassword); check('Real Identity administrator login');
        await page.goto('/AdminOperations/Dashboard'); await shot('admin-dashboard');
        await page.goto('/Product/Index'); await shot('admin-products'); check('Administrator products');
        await page.goto('/Stock/Index'); await shot('admin-stock'); check('Administrator stock');
        await page.goto('/AdminOperations/AllOrders');
        await page.locator('.order-item').first().click();
        await page.locator('select[id^="status-select-"]').selectOption({ label: 'Approved' });
        const updateResponse = page.waitForResponse(r => r.url().includes('/UpdateOrderStatusAjax') && r.request().method() === 'POST');
        await page.locator('.status-update-btn').click();
        assert.equal((await (await updateResponse).json()).success, true);
        check('Administrator status update with CSRF header');
        const paymentResponse = page.waitForResponse(r => r.url().includes('/TogglePaymentStatus') && r.request().method() === 'POST');
        await page.getByRole('button', { name: 'Confirm simulated payment' }).click();
        await paymentResponse;
        await page.waitForFunction(() => Object.values(ordersData).some(o => o.isPaid));
        await page.locator('.order-item').first().click();
        await shot('admin-orders'); check('Administrator simulated payment confirmation');
        check('Administrator order view');
        for (const width of [320, 390, 768, 1440]) { await page.setViewportSize({ width, height: 900 }); await overflow('admin-orders-' + width); }
        check('Responsive widths inspected: 320, 390, 768, 1440');
        await page.setViewportSize({ width: 1440, height: 1000 });
        await page.goto('/AdminOperations/Dashboard'); await shot('admin-dashboard');
        assert.equal(report.resourceErrors.length, 0, 'Local HTTP resource errors detected');
        assert.equal(report.javascriptErrors.length, 0, 'JavaScript errors detected');
        assert.equal(report.overflow.length, 0, 'Horizontal overflow detected');
        check('No JavaScript exceptions or horizontal page overflow');
    } catch (error) {
        report.failure = error.message;
        await page.screenshot({ path: path.join(artifacts, 'browser-failure.png') });
        process.exitCode = 1;
        console.error(error.message);
    } finally {
        fs.writeFileSync(path.join(artifacts, 'browser-report.json'), JSON.stringify(report, null, 2));
        await browser.close();
    }
})();
