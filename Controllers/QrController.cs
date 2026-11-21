using System;
using BOOK.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOOK.Controllers
{
    [Authorize]
    public class QrController : Controller
    {
        private readonly FineQrService _fineQrService;

        public QrController(FineQrService fineQrService)
        {
            _fineQrService = fineQrService;
        }

        // GET: /Qr/Fine?amount=50000&studentCode=24DH113268&cardNumber=LIB-24DH113268&reference=PM0001&format=svg
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Fine(decimal amount, string? studentCode = null, string? cardNumber = null, string? reference = null, string format = "svg")
        {
            if (amount <= 0 || amount > 100_000_000m)
            {
                return BadRequest("Số tiền phạt phải lớn hơn 0 và không vượt quá 100.000.000 VNĐ.");
            }

            var info = _fineQrService.BuildFineQrInfo(amount, studentCode, cardNumber, reference);

            if (string.Equals(format, "png", StringComparison.OrdinalIgnoreCase))
            {
                var pngBytes = _fineQrService.GenerateQrPng(info.VietQrPayload);
                return File(pngBytes, "image/png");
            }

            var svg = _fineQrService.GenerateQrSvg(info.VietQrPayload);
            return Content(svg, "image/svg+xml; charset=utf-8");
        }

        // GET: /Qr/FineInfo?amount=50000&studentCode=24DH113268&cardNumber=LIB-24DH113268&reference=PM0001
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult FineInfo(decimal amount, string? studentCode = null, string? cardNumber = null, string? reference = null)
        {
            if (amount <= 0 || amount > 100_000_000m)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Số tiền phạt phải lớn hơn 0 và không vượt quá 100.000.000 VNĐ."
                });
            }

            var info = _fineQrService.BuildFineQrInfo(amount, studentCode, cardNumber, reference);

            return Json(new
            {
                success = true,
                amount = info.Amount,
                roundedAmount = info.RoundedAmount,
                formattedAmount = info.FormattedAmount,
                tierCode = info.Tier.TierCode,
                tierName = info.Tier.TierName,
                tierRange = info.Tier.TierRange,
                tierBadgeClass = info.Tier.BadgeClass,
                tierAccentColor = info.Tier.AccentColor,
                tierDescription = info.Tier.Description,
                bankBin = info.BankBin,
                bankName = info.BankName,
                accountNumber = info.AccountNumber,
                accountName = info.AccountName,
                transferContent = info.TransferContent,
                vietQrPayload = info.VietQrPayload,
                qrImageUrl = info.QrImageUrl
            });
        }
    }
}
