# Dourak MCP Publication Guide

نعم. وبما أنك **نشرت Dourak بالفعل على ChatGPT Plugins**، فالأفضل الآن أن نفصل القنوات إلى قسمين: قنوات يستفيد منها **المستخدم العادي داخل تطبيقات Chat AI**، وقنوات هدفها الأساسي **المطورون وأدوات البرمجة**.

## 1) منصات الـChat AI والاستخدام العام

هذه هي التي أراها أهم لك الآن، لأن Dourak منتج للمستخدم العادي وليس أداة برمجية:

| المنصة / الدليل | هل المستخدم يستطيع استعمال Dourak داخل Chat؟ | هل يوجد Public Listing؟ | ملاحظتي لـDourak |
|---|---:|---:|---|
| **ChatGPT Plugins** | ✅ نعم | ✅ | **أنت نشرته بالفعل** |
| **Claude Connectors Directory** | ✅ نعم | ✅ | **أولوية عالية جدًا** |
| **Gemini Custom Connected Apps** | ✅ نعم | حاليًا لا أرى public directory واضحًا للـthird-party MCP | مهم لاحقًا |
| **Microsoft 365 Copilot Federated Connectors** | ✅ نعم، لكن بيئة مؤسسات | يوجد Gallery/partner ecosystem | أقل أهمية حاليًا لـDourak |
| **Official MCP Registry** | ليس Chat بحد ذاته | ✅ نعم | **مهم جدًا للاكتشاف** |
| **Smithery** | يمكن استخدام الـMCP منه عبر عدة clients ومنها Claude | ✅ نعم | **مهم للاكتشاف والتوزيع** |
| **Glama** | نعم، ويوفر Connector يمكن ربطه بعملاء Chat مختلفين | ✅ نعم | **مهم للاكتشاف والاختبار** |

والخبر الجديد المهم بخصوص **Gemini**: Google أصبحت تدعم **Custom MCP Apps مباشرة داخل Gemini Web/Mobile**. المستخدم يضع MCP URL ثم يستطيع استدعاء التطبيق باستخدام `@`. لكن الميزة حاليًا مقيدة بحساب Google شخصي، باللغة الإنجليزية، وللمستخدمين في الولايات المتحدة؛ لذلك بالنسبة لك في السعودية لن أجعلها أول أولوية الآن.

### الترتيب الذي أنصحك به الآن

بما أنك أنهيت ChatGPT:

**Claude Directory → Official MCP Registry → Smithery → Glama → Gemini لاحقًا**

---

## 2) أدوات التطوير والـCoding Agents

هذه مفيدة، لكن جمهور Dourak الطبيعي ليس المطورين، لذلك أجعلها مرحلة لاحقة.

| الأداة | MCP support | طريقة الاستخدام العامة |
|---|---:|---|
| **Claude Code** | ✅ | remote/local MCP |
| **Cursor** | ✅ | MCP URL أو config |
| **VS Code / GitHub Copilot** | ✅ | MCP server |
| **Codex** | ✅ | MCP |
| **Windsurf** | ✅ | MCP |
| **JetBrains IDEs** | ✅ | MCP |
| **Zed** | ✅ | MCP |
| **Cline / Roo Code** | ✅ | MCP |
| **Replit** | ✅ | MCP |
| **Gemini CLI** | ✅ | MCP / Extensions |

---

# أولًا: نشر Dourak في Claude Connectors Directory

هنا يوجد تصحيح مهم عن إجابتي السابقة: **إضافة Custom Connector للاستخدام الشخصي لا تعني أنك تستطيع فورًا تقديمه للـpublic directory من حساب فردي**.

Anthropic حاليًا تشترط لتقديم **Remote MCP** إلى Public Connectors Directory أن يكون لديك:

- **Claude Team أو Enterprise organization**
- وأن تكون Owner / Primary Owner أو لديك Directory permission

الحساب الفردي Pro/Max لا يملك Organization Settings اللازمة للتقديم العام.

## قبل التقديم

Dourak يجب أن يكون عنده:

- Remote MCP متاح عبر `HTTPS`
- OAuth 2.0 إذا كان المستخدمون يحتاجون تسجيل دخول
- Documentation page
- Privacy Policy
- Icon
- Support contact
- tool descriptions واضحة
- كل tool عنده `title`
- annotations مثل:
  - `readOnlyHint`
  - `destructiveHint` عند الحاجة

مثلاً أدوات Dourak:

```text
list_circles
title: List saving circles
readOnlyHint: true
```

```text
get_circle
title: Get circle details
readOnlyHint: true
```

أما:

```text
mark_payment_paid
title: Mark contribution as paid
readOnlyHint: false
destructiveHint: false
```

وأي tool يحذف Circle مثلاً:

```text
delete_circle
title: Delete saving circle
readOnlyHint: false
destructiveHint: true
```

## خطوات التقديم

بعد أن يكون لديك Team/Enterprise:

1. ادخل إلى Claude.ai بحساب الـorganization.
2. اذهب إلى **Organization Settings**.
3. افتح **Connectors / Directory submissions**.
4. افتح **Submission Portal**.
5. اختر Remote MCP.
6. أدخل MCP URL مثل:

```text
https://api.dourak.com/mcp
```

7. حدد transport:
   - Streamable HTTP وهو الأفضل الآن
   - أو SSE إذا كان السيرفر لديك كذلك.
8. Claude يقوم بقراءة:
   - tools
   - resources
   - prompts
9. أصلح أي warning متعلق بـtool title أو annotations.
10. أدخل معلومات listing:
   - Name: `Dourak`
   - Tagline
   - Description
   - Categories
   - Documentation URL
   - Privacy Policy URL
   - Support contact
   - Icon
11. أدخل use cases.
12. أدخل معلومات الشركة / المطور.
13. اضبط Authentication.
14. أدخل معلومات Data Handling.
15. أعطهم **Test Account** يحتوي بيانات تجريبية حقيقية.
16. اختبر كل tool بنفسك.
17. أكمل Compliance declarations.
18. Submit.

## نقطة مهمة جدًا لـDourak

اعمل حساب reviewer مثل:

```text
reviewer@dourak.com
```

ويكون فيه مثلاً:

```text
Family Circle
6 members
Monthly amount: 1,000 SAR

Friends Circle
8 members
Monthly amount: 500 SAR
```

وبعض الدفعات Paid وبعضها Pending.

---

# ثانيًا: Official MCP Registry

هذا بالنسبة لي **أهم شيء تعمل عليه بعد Claude**.

الـOfficial MCP Registry هو المصدر vendor-neutral المركزي لمعلومات MCP servers العامة، وليس تابعًا لـClaude أو OpenAI.

## لماذا مهم؟

لأن downstream registries تستطيع اكتشاف Dourak منه.

يعني بدل نشر Dourak يدويًا في كل مكان، وجودك هنا يعطيك canonical identity مثل:

```text
io.github.yourusername/dourak
```

أو الأفضل عندما يكون عندك domain:

```text
com.dourak/mcp
```

## الخيار الأفضل لـDourak

بما أنك لديك Remote MCP وليس npm package فقط، استخدم domain namespace إذا كنت تملك domain Dourak.

مثلاً:

```text
com.dourak/mcp
```

### الخطوات

ثبت `mcp-publisher`.

على Windows PowerShell:

```powershell
mcp-publisher --help
```

ثم في مشروع MCP:

```powershell
mcp-publisher init
```

هذا ينشئ:

```text
server.json
```

ثم عدّل الملف.

فكرته ستكون تقريبًا:

```json
{
  "name": "com.dourak/mcp",
  "description": "Manage and interact with Dourak social saving circles through AI assistants.",
  "version": "1.0.0"
}
```

وتضيف إليه remote transport/URL حسب schema الذي يولده لك `mcp-publisher init`.

لا تنسخ schema من مثال قديم؛ استخدم `init` لأن الـRegistry ما زال Preview والـschema يتطور.

بعد ذلك:

```powershell
mcp-publisher validate
```

ثم authenticate.

إذا استخدمت GitHub namespace:

```powershell
mcp-publisher login github
```

أما إذا استخدمت:

```text
com.dourak/*
```

فتستخدم domain verification المناسبة.

وأخيرًا:

```powershell
mcp-publisher publish
```

## ملاحظة

الـOfficial Registry **لا يستضيف Dourak MCP**.

هو يستضيف metadata فقط:

```text
Official Registry
      ↓
Dourak metadata
      ↓
https://api.dourak.com/mcp
```

والـMCP الحقيقي يبقى مستضاف عندك.

---

# ثالثًا: Smithery

Smithery أكثر من مجرد directory. هو discovery + connection management + OAuth/session handling + observability، ويدعم servers مستضافة خارجيًا أيضًا.

وبما أن Dourak موجود عندك أصلًا:

```text
https://api.dourak.com/mcp
```

لا تحتاج أن تجعل Smithery يستضيف الكود.

## خطوات النشر

1. ادخل إلى Smithery.
2. Sign in.
3. اختر **Publish MCP Server**.
4. أنشئ namespace خاص بك، مثلاً:

```text
dourak
```

ويمكن عمل namespace أيضًا من CLI:

```bash
smithery namespace create dourak
```

5. اختر نشر **existing/remote MCP server** وليس استضافة server جديد.
6. أدخل endpoint:

```text
https://api.dourak.com/mcp
```

7. أدخل:
   - Name: Dourak
   - Description
   - Homepage
   - Documentation
   - authentication details
8. دع Smithery يتصل بالسيرفر ويقرأ tools.
9. اختبر tools.
10. Publish.

بعد النشر يمكن أن يصبح لك identifier شبيهًا بـ:

```text
dourak/dourak
```

## CLI مفيد للاختبار

```bash
npm install -g @smithery/cli
```

ثم:

```bash
smithery auth login
```

ثم لاحقًا:

```bash
smithery mcp add dourak/dourak
```

ثم:

```bash
smithery tool list dourak
```

---

# رابعًا: Glama

بالنسبة لـDourak تحديدًا اختر **Connector** وليس open-source MCP Server.

Glama يفرق بين:

**Server**
= GitHub/open source implementation

و:

**Connector**
= Remote MCP موجود عندك على الإنترنت.

وحالتك الثانية تمامًا.

## خطوات النشر

1. اذهب إلى Glama MCP Connectors.
2. Sign in.
3. افتح **Connectors**.
4. اختر **Add MCP Server**.
5. اختر **Connector**.
6. اكتب:
   - Name: Dourak
   - Description
7. أدخل:

```text
https://api.dourak.com/mcp
```

8. يجب أن يكون:
   - HTTPS
   - Streamable HTTP
9. إذا يحتاج Authentication:
   - أعط Glama test credentials
   - أو إذا يدعم OAuth 2.1 Dynamic Client Registration فلا تحتاج test credentials يدوية.
10. Glama يعمل health check.
11. إذا أصبح **Healthy**، يظهر في البحث العام.

## بعد ذلك Claim Ownership

هذه خطوة أنصحك تعملها بالتأكيد.

افتح صفحة Dourak في Glama:

**Claim ownership**

ثم اختر واحدة:

- GitHub
- HTTP verification
- DNS verification

أسهل طريقة مع domain هي HTTP.

Glama تعطيك token وتطلب وضع ملف:

```text
https://api.dourak.com/.well-known/glama.json
```

بمحتوى شبيه:

```json
{
  "$schema": "https://glama.ai/mcp/schemas/connector.json",
  "claim": "glama_claim_..."
}
```

ثم تضغط **Check HTTP challenge**.

بعدها تستطيع إدارة:

- listing
- health
- thumbnails
- analytics

---

# ملاحظة مهمة جدًا: Official Registry قد يوفر عليك جزءًا من Glama

Glama تستطيع استخدام listing الموجود في **Official MCP Registry**، وإذا كان connector مرتبطًا به، تستطيع مزامنة:

- Name
- Description
- URL

من الـOfficial Registry.

لذلك ترتيب التنفيذ الأفضل عندك هو:

```text
Dourak MCP
     │
     ├── ChatGPT Plugins       ✅ Done
     │
     ├── Claude Directory      ← next
     │
     ├── Official MCP Registry
     │          │
     │          └── Glama can discover/sync
     │
     ├── Smithery
     │
     └── Glama
```

# ما الذي أجهزه مرة واحدة قبل الأربعة؟

جهز هذه الأشياء الآن مرة واحدة، لأنها ستتكرر في كل submissions:

| العنصر | مثال لـDourak |
|---|---|
| MCP URL | `https://api.dourak.com/mcp` |
| Product name | Dourak |
| Short tagline | Organize social saving circles with AI |
| Long description | 300–1000 كلمة مختصرة وواضحة |
| Icon | Dourak app icon |
| Website | `https://dourak.com` |
| Documentation | `https://dourak.com/docs/mcp` |
| Privacy Policy | `https://dourak.com/privacy` |
| Terms | `https://dourak.com/terms` |
| Support | `support@dourak.com` |
| OAuth | OAuth 2.0 / DCR إن أمكن |
| Test account | حساب reviewer |
| Tool annotations | read-only/write/destructive |
| MCP namespace | `com.dourak/mcp` مثلاً |

**أهم خطوتين تقنيًا قبل أن تبدأ بالنشر:** تأكد أن Dourak يدعم **OAuth 2.0 بشكل مناسب للـMCP**، وأن كل tool عنده `title` وannotations صحيحة. هذا بالذات سيجعل Claude وregistries الأخرى أسهل بكثير في المراجعة والاستخدام.
