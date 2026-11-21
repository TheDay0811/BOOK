using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using QRCoder;

namespace BOOK.Services
{
    public class FineTierInfo
    {
        public string TierCode { get; set; } = "LEVEL_1";
        public string TierName { get; set; } = string.Empty;
        public string TierRange { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = string.Empty;
        public string AccentColor { get; set; } = "amber";
        public string Description { get; set; } = string.Empty;
    }

    public class FineQrInfoResult
    {
        public decimal Amount { get; set; }
        public long RoundedAmount { get; set; }
        public string FormattedAmount { get; set; } = string.Empty;
        public FineTierInfo Tier { get; set; } = new();
        public string BankBin { get; set; } = "970436";
        public string BankName { get; set; } = "Vietcombank (VCB)";
        public string AccountNumber { get; set; } = "102888666999";
        public string AccountName { get; set; } = "THU VIEN DAI HOC HUFLIT";
        public string TransferContent { get; set; } = string.Empty;
        public string VietQrPayload { get; set; } = string.Empty;
        public string QrImageUrl { get; set; } = string.Empty;
    }

    public class FineQrService
    {
        private readonly IConfiguration _configuration;

        public FineQrService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public FineTierInfo GetFineTier(decimal amount)
        {
            if (amount < 50000m)
            {
                return new FineTierInfo
                {
                    TierCode = "LEVEL_1",
                    TierName = "Mức 1 • Phạt nhẹ / Trễ hạn",
                    TierRange = "Dưới 50.000 VNĐ",
                    BadgeClass = "bg-amber-100 text-amber-800 border-amber-300",
                    AccentColor = "amber",
                    Description = "Áp dụng cho khoản phạt trễ hạn (2.000đ/ngày/cuốn). Quét mã QR với số tiền được gán sẵn để thanh toán."
                };
            }

            if (amount < 100000m)
            {
                return new FineTierInfo
                {
                    TierCode = "LEVEL_2",
                    TierName = "Mức 2 • Bồi thường hư hỏng",
                    TierRange = "50.000 - 99.999 VNĐ",
                    BadgeClass = "bg-orange-100 text-orange-900 border-orange-300",
                    AccentColor = "orange",
                    Description = "Áp dụng cho sách hư hỏng / rách trang (định mức 50.000đ/cuốn) hoặc cộng dồn trễ hạn."
                };
            }

            return new FineTierInfo
            {
                TierCode = "LEVEL_3",
                TierName = "Mức 3 • Bồi thường mất sách / Mức cao",
                TierRange = "Từ 100.000 VNĐ trở lên",
                BadgeClass = "bg-rose-100 text-rose-900 border-rose-300",
                AccentColor = "rose",
                Description = "Áp dụng cho mất tài liệu (100.000đ/cuốn) hoặc nhiều khoản bồi thường. Thẻ mở khóa ngay sau khi xác nhận thu đủ."
            };
        }

        public FineQrInfoResult BuildFineQrInfo(decimal amount, string? studentCode, string? cardNumber, string? reference)
        {
            var rounded = (long)Math.Round(Math.Max(0m, amount), 0, MidpointRounding.AwayFromZero);
            var bankBin = _configuration["PaymentSettings:BankBin"] ?? "970436";
            var bankName = _configuration["PaymentSettings:BankName"] ?? "Vietcombank (VCB)";
            var accountNumber = _configuration["PaymentSettings:AccountNumber"] ?? "102888666999";
            var accountName = _configuration["PaymentSettings:AccountName"] ?? "THU VIEN DAI HOC HUFLIT";

            var transferContent = BuildTransferMemo(studentCode, cardNumber, reference, rounded);
            var payload = BuildVietQrPayload(bankBin, accountNumber, rounded, transferContent);

            var encodedStudent = Uri.EscapeDataString(studentCode ?? "");
            var encodedCard = Uri.EscapeDataString(cardNumber ?? "");
            var encodedRef = Uri.EscapeDataString(reference ?? "");

            return new FineQrInfoResult
            {
                Amount = amount,
                RoundedAmount = rounded,
                FormattedAmount = $"{rounded:N0} VNĐ",
                Tier = GetFineTier(amount),
                BankBin = bankBin,
                BankName = bankName,
                AccountNumber = accountNumber,
                AccountName = accountName,
                TransferContent = transferContent,
                VietQrPayload = payload,
                QrImageUrl = $"/Qr/Fine?amount={rounded}&studentCode={encodedStudent}&cardNumber={encodedCard}&reference={encodedRef}"
            };
        }

        public string GenerateQrSvg(string payload)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var svgQr = new SvgQRCode(qrData);
            return svgQr.GetGraphic(8, "#0f172a", "#ffffff", drawQuietZones: true);
        }

        public byte[] GenerateQrPng(string payload)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var pngQr = new PngByteQRCode(qrData);
            return pngQr.GetGraphic(10);
        }

        public static string BuildTransferMemo(string? studentCode, string? cardNumber, string? reference, long roundedAmount)
        {
            var idPart = !string.IsNullOrWhiteSpace(studentCode)
                ? studentCode
                : (!string.IsNullOrWhiteSpace(cardNumber) ? cardNumber : "HUFLIT");

            var refPart = string.IsNullOrWhiteSpace(reference) ? "" : $" {reference}";
            var raw = $"NOPPHAT {idPart}{refPart} {roundedAmount}D";
            var ascii = ToAscii(raw).ToUpperInvariant();
            ascii = Regex.Replace(ascii, @"[^A-Z0-9\s\-]", "");
            ascii = Regex.Replace(ascii, @"\s+", " ").Trim();
            if (ascii.Length > 50)
            {
                ascii = ascii[..50].Trim();
            }
            return ascii;
        }

        public static string BuildVietQrPayload(string bankBin, string accountNumber, long amount, string purpose)
        {
            static string Tlv(string tag, string val) => $"{tag}{val.Length:D2}{val}";

            var cleanBin = Regex.Replace(bankBin ?? "970436", @"[^0-9]", "");
            var cleanAcc = Regex.Replace(accountNumber ?? "102888666999", @"[^A-Za-z0-9]", "");

            var beneficiaryOrg = Tlv("00", cleanBin) + Tlv("01", cleanAcc);
            var merchantInfo = Tlv("00", "A000000727")
                             + Tlv("01", beneficiaryOrg)
                             + Tlv("02", "QRIBFTTA");

            var sb = new StringBuilder();
            sb.Append(Tlv("00", "01"));
            sb.Append(Tlv("01", amount > 0 ? "12" : "11"));
            sb.Append(Tlv("38", merchantInfo));
            sb.Append(Tlv("53", "704"));
            if (amount > 0)
            {
                sb.Append(Tlv("54", amount.ToString(CultureInfo.InvariantCulture)));
            }
            sb.Append(Tlv("58", "VN"));
            if (!string.IsNullOrWhiteSpace(purpose))
            {
                sb.Append(Tlv("62", Tlv("08", purpose)));
            }
            sb.Append("6304");

            var crc = ComputeCrc16Ccitt(sb.ToString());
            sb.Append(crc.ToString("X4", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        public static ushort ComputeCrc16Ccitt(string input)
        {
            ushort crc = 0xFFFF;
            var bytes = Encoding.UTF8.GetBytes(input);
            foreach (var b in bytes)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }
            return crc;
        }

        private static string ToAscii(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var replaced = input.Replace('đ', 'd').Replace('Đ', 'D');
            var normalized = replaced.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
