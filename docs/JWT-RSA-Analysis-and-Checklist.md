# JWT RSA Analizi və Düzəliş Checklist

## 1. Nə eksikdir / Nə səhvdir (konkret maddələr)

### AuthService
| # | Problem | Status |
|---|--------|--------|
| 1 | **Issuer/Audience hardcoded** – TokenService-də `"myada-auth"` və `"myada-api"` sabit yazılıb; config-dən oxunmalıdır. | ✅ Düzəldildi – `JwtOptions` + appsettings `Jwt` section |
| 2 | **Private key path hardcoded** – `keys/private_key.pem` Program.cs-də sabit; mühit üçün config olmalıdır. | ✅ Düzəldildi – `Jwt:PrivateKeyPath` |
| 3 | **`nbf` (notBefore) yoxdur** – Token yalnız `exp` ilə; `nbf` əlavə etmək best practice-dir. | ✅ Düzəldildi – `notBefore: now` əlavə olundu |
| 4 | **Token lifetime sabit** – 2 saat hardcoded; config-dən gəlməlidir. | ✅ Düzəldildi – `JwtOptions.TokenLifetimeHours` |
| 5 | **appsettings-də JWT config yox idi** – Issuer, Audience, key path yalnız koddan. | ✅ Düzəldildi – `appsettings.json`-a `Jwt` section |

### LostFoundService
| # | Problem | Status |
|---|--------|--------|
| 6 | **ClockSkew təyin olunmayıb** – Default 5 dəq; vaxtı bitmiş token 5 dəq ərzində qəbul oluna bilərdi. | ✅ Düzəldildi – `ClockSkew = TimeSpan.Zero` |
| 7 | **userId claim – yalnız NameIdentifier** – JWT-də `sub` gəlir; .NET 8-də mapping fərqli ola bilər, hər iki claim yoxlanmalıdır. | ✅ Düzəldildi – Extension-da `sub` + `NameIdentifier` |
| 8 | **JWKS olmadan alternativ yox** – Auth service əlçatan olmanda startup uğursuz olurdu; public key faylla fallback lazımdır. | ✅ Düzəldildi – `Auth:PublicKeyPath` ilə lokal PEM |
| 9 | **Issuer/Audience hardcoded** – ValidIssuer/ValidAudience config-dən oxunmalıdır. | ✅ Düzəldildi – `Auth:Issuer`, `Auth:Audience` |
| 10 | **MapInboundClaims** – `sub` claim-inin `ClaimTypes.NameIdentifier`-a map olub-olmaması versiyadan asılıdır; açıq “sub” üçün `MapInboundClaims = false` təhlükəsizdir. | ✅ Düzəldildi – `options.MapInboundClaims = false` |

### Ümumi
| # | Problem | Status |
|---|--------|--------|
| 11 | **Expired token** – `ValidateLifetime = true` artıq var; vaxtı keçən token üçün middleware avtomatik 401 qaytarır. | ✅ Dəyişiklik tələb olunmur |
| 12 | **AuthService-də `UseAuthentication()`** – İndi auth endpoint-ləri public olsa da, gələcək üçün `UseAuthentication()` əlavə etmək məsləhətdir. | ⚪ Opsional – ehtiyac olsa əlavə edin |

---

## 2. Hansı claim userId olmalıdır (sub vs NameIdentifier) və necə oxumalı

- **JWT standartı:** `sub` (subject) = istifadəçi identifikatoru (userId). AuthService artıq `JwtRegisteredClaimNames.Sub` (yəni `user.Id`) yazır – **düzgündür**.
- **NameIdentifier:** ASP.NET Core bəzi konfiqurasiyalarda JWT-dən gələn `sub`-ı `ClaimTypes.NameIdentifier` (uzun URI) kimi map edir. Bu davranış `MapInboundClaims`-dan asılıdır.
- **Tövsiyə:** Həm `sub`, həm də `NameIdentifier`-ı yoxlayan extension istifadə et (indi belə edilib). LostFoundService-də `MapInboundClaims = false` ilə claim-lər “raw” qalır, ona görə əsasən `sub` istifadə olunacaq.

**Controller/Service-də userId oxumaq:**

```csharp
// Controller
[Authorize]
[HttpGet("users/me")]
public async Task<IActionResult> GetMyFoundItems()
{
    var userId = User.GetUserId();  // ClaimsPrincipal extension
    var resultDto = await _service.GetMyFoundItemsAsync(userId);
    return Ok(resultDto);
}
```

**Extension (artıq düzəldilib):**

```csharp
// sub əvvəl, sonra NameIdentifier (ASP.NET mapping üçün)
var userId = user.FindFirst(SubClaimType)?.Value   // "sub"
             ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

Service layer-ə userId parametr kimi ötürün (controller `User.GetUserId()` oxuyub servisə verir) – servisə `IHttpContextAccessor` və ya `ClaimsPrincipal` inject etməyin ehtiyacı yoxdur.

---

## 3. Program.cs / appsettings – Issuer, Audience, SigningKey (RSA public key)

### AuthService (token yaradan)

- **appsettings.json** – düzgün qurulub:
  - `Jwt:Issuer`, `Jwt:Audience`, `Jwt:TokenLifetimeHours`, `Jwt:PrivateKeyPath`
- **Program.cs** – JWT config section oxunur, key path config-dən gəlir (default: `keys/private_key.pem`).

### LostFoundService (token yoxlayan)

- **appsettings.json** – düzgün qurulub:
  - `Auth:Issuer`, `Auth:Audience` – ValidIssuer/ValidAudience üçün
  - `Auth:JwksUrl` – JWKS istifadə etmək üçün
  - `Auth:PublicKeyPath` – JWKS istifadə etməmək üçün (boş buraxılsa yalnız JwksUrl istifadə olunur)
- **Public key format:** PEM fayl (`-----BEGIN PUBLIC KEY-----` … `-----END PUBLIC KEY-----`). LostFoundService eyni `public_key.pem`-i AuthService ilə paylaşmalıdır (kopya və ya paylanmış config).

---

## 4. ValidateLifetime və ClockSkew

- **ValidateLifetime = true** – artıq açıqdır; `exp` və `nbf` yoxlanır.
- **ClockSkew:**
  - **Default:** 5 dəqiqə – vaxtı bitən token 5 dəq ərzində etibarlı qəbul olunur.
  - **Tövsiyə (sənin tələbinə uyğun):** `ClockSkew = TimeSpan.Zero` – vaxtı bitən token dərhal 401 alır.
  - Alternativ: kiçik tolerance üçün məs. `TimeSpan.FromSeconds(30)`.

Dəyişiklik (LostFoundService Program.cs):

```csharp
ValidateLifetime = true,
ClockSkew = TimeSpan.Zero,
```

---

## 5. JWKS olmadan ən düzgün yol (public key ilə config)

JWKS (/.well-known/jwks.json) istifadə etmək istəmirsənsə:

1. **Public key faylı** – AuthService ilə eyni RSA cütlüyünün **public** açarı (PEM).
2. **LostFoundService appsettings:**
   - `Auth:JwksUrl` – boş burax və ya sil.
   - `Auth:PublicKeyPath` – public key PEM faylının yolu (məs. `Keys/public_key.pem` və ya Docker/deploy üçün absolute path).
3. **Startup ardıcıllığı (artıq tətbiq olunub):**
   - Əgər `Auth:PublicKeyPath` dolu və fayl mövcuddursa → PEM oxunur, `IssuerSigningKey` təyin olunur (JWKS çağrılmır).
   - Əks halda `Auth:JwksUrl` dolu olmalıdır → JWKS ilə açarlar yüklənir.
   - Hər ikisi uyğun deyilsə startup-da aydın xəta verilir.

Nümunə (yalnız public key, JWKS yox):

```json
"Auth": {
  "Issuer": "myada-auth",
  "Audience": "myada-api",
  "JwksUrl": "",
  "PublicKeyPath": "Keys/public_key.pem"
}
```

Docker/deploy-da path-i mühitə görə (məs. `Auth__PublicKeyPath`) vermək olar.

---

## 6. Checklist (sonda yoxlama siyahısı)

| Maddə | Yoxlama |
|-------|--------|
| **Issuer** | AuthService və LostFoundService-də eyni (məs. `myada-auth`), hər ikisi config-dən oxunur. |
| **Audience** | Eyni (məs. `myada-api`), config-dən. |
| **exp** | AuthService-də token `exp` ilə yaradılır; LostFoundService-də `ValidateLifetime = true`. |
| **nbf** | AuthService-də `notBefore: now` əlavə olundu. |
| **ClockSkew** | LostFoundService-də `TimeSpan.Zero` (vaxtı bitən token dərhal 401). |
| **Public key format** | PEM; LostFoundService `ImportFromPem` ilə oxuyur (JWKS və ya PublicKeyPath). |
| **Claim mapping** | userId üçün `sub` (və ehtiyat üçün NameIdentifier); extension hər ikisini yoxlayır. |
| **[Authorize]** | Qorunmalı endpoint-lərdə `[Authorize]` var; controller-də `User.GetUserId()` çağrılır. |
| **401 vaxtı bitəndə** | JWT Bearer middleware `ValidateLifetime` + `ClockSkew.Zero` ilə avtomatik 401 qaytarır. |
| **JWKS vs PublicKeyPath** | Ya `Auth:JwksUrl`, ya `Auth:PublicKeyPath` (PEM fayl) təmin edilməlidir; hər ikisi düzgün konfiqurasiya olunub. |

---

## 7. Qısa “diff” xülasəsi

- **AuthService:** `JwtOptions` (config), TokenService-də issuer/audience/lifetime/nbf config-dən, private key path config-dən, appsettings `Jwt` section.
- **LostFoundService:** `ClaimsPrincipalExtensions` – `sub` + NameIdentifier; Program.cs – ClockSkew.Zero, MapInboundClaims = false, Auth:Issuer/Audience, PublicKeyPath fallback; appsettings Auth section.
- **Controller/Service:** userId üçün yalnız `User.GetUserId()` istifadə et; servisə parametr kimi ötür.

Bu addımlarla JWT RSA axını (sign/verify, issuer, audience, exp/nbf, 401, userId claim) tələblərə uyğundur.
