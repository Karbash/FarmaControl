using System.Globalization;
using System.Text;
using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Application.Audit.Models;
using FarmaControl.Domain.Audit;

namespace FarmaControl.Infrastructure.Audit;

public sealed class SimpleAuditReportPdfGenerator : IAuditReportPdfGenerator
{
    private const int RowsPerPage = 24;

    public Task<byte[]> GenerateAsync(
        IReadOnlyList<AuditLog> logs,
        AuditLogFilterModel filter,
        string generatedBy,
        CancellationToken cancellationToken)
    {
        var streams = new List<string>
        {
            BuildSignaturePage(logs, filter, generatedBy)
        };

        AuditLog[] orderedLogs = logs
            .OrderBy(log => log.CreatedAt)
            .ThenBy(log => log.Id)
            .ToArray();

        if (orderedLogs.Length == 0)
        {
            streams.Add(BuildEventsPage([], 1, 1));
        }
        else
        {
            int pageCount = (int)Math.Ceiling(orderedLogs.Length / (decimal)RowsPerPage);
            for (int index = 0; index < pageCount; index++)
            {
                streams.Add(BuildEventsPage(
                    orderedLogs.Skip(index * RowsPerPage).Take(RowsPerPage).ToArray(),
                    index + 1,
                    pageCount));
            }
        }

        return Task.FromResult(BuildPdf(streams));
    }

    private static string BuildSignaturePage(
        IReadOnlyList<AuditLog> logs,
        AuditLogFilterModel filter,
        string generatedBy)
    {
        var page = new PdfPageBuilder();

        page.Rect(32, 34, 531, 774);
        page.Text(48, 785, "RELATORIO DE AUDITORIA PARA ASSINATURA", 14, bold: true);
        page.Text(410, 785, $"Total: {logs.Count}", 10, bold: true);
        page.Line(32, 772, 563, 772);

        page.Section(48, 744, "RANGE E FILTROS");
        page.Field(48, 706, 150, 26, "Inicio", Value(filter.StartDate));
        page.Field(206, 706, 150, 26, "Fim", Value(filter.EndDate));
        page.Field(364, 706, 182, 26, "Gerado por", Value(generatedBy));
        page.Field(48, 670, 150, 26, "Acao", Value(filter.Action));
        page.Field(206, 670, 150, 26, "Entidade", Value(filter.Entity));
        page.Field(364, 670, 182, 26, "Usuario", Value(filter.User));
        page.Field(48, 634, 220, 26, "Gerado em", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        page.Field(276, 634, 270, 26, "Observacao", "Assinar apos conferencia do periodo");

        page.Section(48, 578, "TERMO DE CONFERENCIA");
        page.TextArea(
            48,
            466,
            498,
            90,
            "Declaracao",
            "Declaro que conferi os eventos de auditoria listados neste relatorio para o range informado acima.");

        page.Section(48, 408, "ASSINATURAS");
        page.SignatureLine(48, 330, 220, "Responsavel pela auditoria");
        page.SignatureLine(326, 330, 220, "Conferente");
        page.SignatureLine(48, 242, 220, "Administrador");
        page.SignatureLine(326, 242, 220, "Data da assinatura");

        page.Section(48, 172, "RESUMO");
        page.Field(48, 134, 120, 26, "Eventos", logs.Count.ToString(CultureInfo.InvariantCulture));
        page.Field(176, 134, 170, 26, "Primeiro evento", Value(logs.OrderBy(log => log.CreatedAt).FirstOrDefault()?.CreatedAt));
        page.Field(354, 134, 192, 26, "Ultimo evento", Value(logs.OrderByDescending(log => log.CreatedAt).FirstOrDefault()?.CreatedAt));

        return page.ToString();
    }

    private static string BuildEventsPage(
        IReadOnlyList<AuditLog> logs,
        int pageNumber,
        int pageCount)
    {
        var page = new PdfPageBuilder();

        page.Rect(32, 34, 531, 774);
        page.Text(48, 785, "RELATORIO DE AUDITORIA - EVENTOS", 14, bold: true);
        page.Text(432, 785, $"Pagina {pageNumber}/{pageCount}", 9, bold: true);
        page.Line(32, 772, 563, 772);

        page.AuditTable(48, 742, logs);

        if (logs.Count == 0)
        {
            page.Text(56, 700, "Nenhum evento encontrado para os filtros informados.", 10);
        }

        return page.ToString();
    }

    private static byte[] BuildPdf(IReadOnlyList<string> pageStreams)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>"
        };

        string kids = string.Join(
            " ",
            Enumerable.Range(0, pageStreams.Count).Select(index => $"{3 + index * 2} 0 R"));

        objects.Add($"<< /Type /Pages /Kids [{kids}] /Count {pageStreams.Count} >>");

        for (int index = 0; index < pageStreams.Count; index++)
        {
            int pageObject = 3 + index * 2;
            int contentObject = pageObject + 1;
            string stream = pageStreams[index];

            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> /F2 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >> >> >> /Contents {contentObject} 0 R >>");
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
        }

        return WritePdf(objects);
    }

    private static byte[] WritePdf(IReadOnlyList<string> objects)
    {
        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n");

        var offsets = new List<long> { 0 };
        for (int index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        long xrefPosition = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");

        foreach (long offset in offsets.Skip(1))
        {
            WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF");

        return stream.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        stream.Write(Encoding.ASCII.GetBytes(value));
    }

    private static string Value(object? value)
    {
        return value switch
        {
            null => "Todos",
            "" => "Todos",
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTimeOffset dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "Todos"
        };
    }

    private sealed class PdfPageBuilder
    {
        private readonly StringBuilder builder = new();

        public void Section(int x, int y, string title)
        {
            Rect(x, y - 16, 498, 22, fillGray: "0.92");
            Text(x + 8, y - 10, title, 10, bold: true);
        }

        public void Field(int x, int y, int width, int height, string label, string value)
        {
            Rect(x, y, width, height);
            Text(x + 5, y + height - 9, label, 6, bold: true);
            Text(x + 5, y + 7, Truncate(value, Math.Max(8, width / 5)), 9);
        }

        public void TextArea(int x, int y, int width, int height, string label, string value)
        {
            Rect(x, y, width, height);
            Text(x + 6, y + height - 12, label, 8, bold: true);

            int lineY = y + height - 28;
            foreach (string line in Wrap(value, Math.Max(20, width / 5)).Take(Math.Max(1, (height - 24) / 12)))
            {
                Text(x + 6, lineY, line, 8);
                lineY -= 12;
            }
        }

        public void SignatureLine(int x, int y, int width, string label)
        {
            Line(x, y, x + width, y);
            Text(x, y - 16, label, 8, bold: true);
        }

        public void AuditTable(int x, int y, IReadOnlyList<AuditLog> logs)
        {
            int rowHeight = 26;
            int[] widths = [72, 92, 54, 72, 198, 50];
            string[] headers = ["Data", "Usuario", "Acao", "Entidade", "Descricao", "Id"];

            int tableWidth = widths.Sum();
            Rect(x, y - rowHeight, tableWidth, rowHeight, fillGray: "0.96");

            int columnX = x;
            for (int index = 0; index < headers.Length; index++)
            {
                if (index > 0)
                {
                    Line(columnX, y - rowHeight, columnX, y);
                }

                Text(columnX + 5, y - 17, headers[index], 7, bold: true);
                columnX += widths[index];
            }

            for (int index = 0; index < logs.Count; index++)
            {
                AuditLog log = logs[index];
                int rowY = y - ((index + 2) * rowHeight);
                Rect(x, rowY, tableWidth, rowHeight);

                string[] values =
                [
                    log.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    log.UserName,
                    log.Action,
                    $"{log.Entity} {Value(log.EntityId)}",
                    log.Description,
                    log.Id.ToString(CultureInfo.InvariantCulture)
                ];

                columnX = x;
                for (int column = 0; column < values.Length; column++)
                {
                    if (column > 0)
                    {
                        Line(columnX, rowY, columnX, rowY + rowHeight);
                    }

                    Text(columnX + 5, rowY + 10, Truncate(values[column], Math.Max(4, widths[column] / 5)), 7);
                    columnX += widths[column];
                }
            }
        }

        public void Text(int x, int y, string value, int size, bool bold = false)
        {
            string font = bold ? "F2" : "F1";
            builder.Append(CultureInfo.InvariantCulture, $"BT /{font} {size} Tf {x} {y} Td ({Escape(value)}) Tj ET\n");
        }

        public void Rect(int x, int y, int width, int height, string? fillGray = null)
        {
            if (fillGray is not null)
            {
                builder.Append(CultureInfo.InvariantCulture, $"{fillGray} g {x} {y} {width} {height} re f 0 g\n");
            }

            builder.Append(CultureInfo.InvariantCulture, $"0.75 w {x} {y} {width} {height} re S\n");
        }

        public void Line(int x1, int y1, int x2, int y2)
        {
            builder.Append(CultureInfo.InvariantCulture, $"0.75 w {x1} {y1} m {x2} {y2} l S\n");
        }

        public override string ToString()
        {
            return builder.ToString();
        }

        private static IEnumerable<string> Wrap(string value, int maxLength)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

            while (text.Length > maxLength)
            {
                int split = text.LastIndexOf(' ', maxLength);
                if (split <= 0)
                {
                    split = maxLength;
                }

                yield return text[..split];
                text = text[split..].TrimStart();
            }

            yield return text;
        }

        private static string Truncate(string value, int maxLength)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
            return text.Length <= maxLength ? text : text[..Math.Max(0, maxLength - 3)] + "...";
        }

        private static string Escape(string value)
        {
            var escaped = new StringBuilder(value.Length);

            foreach (char character in value)
            {
                escaped.Append(character switch
                {
                    '(' => "\\(",
                    ')' => "\\)",
                    '\\' => "\\\\",
                    >= ' ' and <= '~' => character,
                    _ => '?'
                });
            }

            return escaped.ToString();
        }
    }
}
