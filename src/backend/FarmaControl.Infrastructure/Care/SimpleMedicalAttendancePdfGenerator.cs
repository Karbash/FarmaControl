using System.Globalization;
using System.Text;
using FarmaControl.Application.Care.MedicalAttendances;
using FarmaControl.Domain.Care;

namespace FarmaControl.Infrastructure.Care;

public sealed class SimpleMedicalAttendancePdfGenerator : IMedicalAttendancePdfGenerator
{
    public Task<byte[]> GenerateAsync(
        MedicalAttendance attendance,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> streams =
        [
            BuildIdentificationPage(attendance),
            BuildProceduresPage(attendance),
            BuildTriageSupportPage(attendance),
            BuildResponsiblesPage(attendance)
        ];

        return Task.FromResult(BuildPdf(streams));
    }

    private static string BuildIdentificationPage(MedicalAttendance attendance)
    {
        var page = new PdfPageBuilder();

        page.Rect(32, 34, 531, 774);
        page.Text(48, 785, "FICHA DE ATENDIMENTO MEDICO", 15, bold: true);
        page.Text(380, 785, $"Ficha: {attendance.Id}", 10, bold: true);
        page.Line(32, 772, 563, 772);

        page.Text(48, 754, "IDENTIFICACAO", 10, bold: true);
        page.Field(48, 724, 330, 24, "Nome", attendance.Name);
        page.Field(386, 724, 58, 24, "Idade", Value(attendance.Age));
        page.Field(452, 724, 94, 24, "Tipo", attendance.AttendanceType.ToString());
        page.Field(48, 690, 132, 24, "Data", attendance.AttendanceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        page.Field(188, 690, 96, 24, "Horario", Value(attendance.AttendanceTime));
        page.Field(292, 690, 136, 24, "Cidade", Value(attendance.City));
        page.Field(436, 690, 110, 24, "Retorno", Value(attendance.ReturnNumber));

        page.Section(48, 646, "IGREJA");
        page.Field(48, 612, 250, 24, "Igreja", Value(attendance.Church));
        page.Field(306, 612, 240, 24, "Pastor", Value(attendance.Pastor));

        page.Section(48, 568, "SINAIS VITAIS");
        page.Field(48, 534, 86, 24, "PA sistolica", Value(attendance.VitalSigns.SystolicPressure));
        page.Field(142, 534, 86, 24, "PA diastolica", Value(attendance.VitalSigns.DiastolicPressure));
        page.Field(236, 534, 86, 24, "Temp.", Value(attendance.VitalSigns.Temperature));
        page.Field(330, 534, 86, 24, "Glicemia", Value(attendance.VitalSigns.BloodGlucose));
        page.Field(424, 534, 56, 24, "SpO2", Value(attendance.VitalSigns.OxygenSaturation));
        page.Field(488, 534, 58, 24, "HR", Value(attendance.VitalSigns.HeartRate));

        page.Section(48, 490, "HISTORICO CLINICO");
        page.TextArea(48, 416, 498, 56, "Queixa principal", Value(attendance.ChiefComplaint));
        page.TextArea(48, 328, 498, 70, "HPP", Value(attendance.PreviousPathologicalHistory));
        page.TextArea(48, 240, 498, 70, "HDA", Value(attendance.CurrentDiseaseHistory));
        page.TextArea(48, 158, 498, 64, "Exame fisico", Value(attendance.PhysicalExam));
        page.TextArea(48, 92, 330, 48, "Hipotese diagnostica", Value(attendance.DiagnosticHypothesis));
        page.Field(386, 116, 160, 24, "CID-10", Cid10Value(attendance));

        page.Text(48, 38, $"Criado em: {attendance.CreatedAt:yyyy-MM-dd HH:mm:ss}", 8);

        return page.ToString();
    }

    private static string BuildTriageSupportPage(MedicalAttendance attendance)
    {
        var page = new PdfPageBuilder();

        page.Rect(32, 34, 531, 774);
        page.Text(48, 785, "PARECER DE APOIO DA TRIAGEM", 14, bold: true);
        page.Text(390, 785, $"Ficha: {attendance.Id}", 10, bold: true);
        page.Line(32, 772, 563, 772);

        page.Field(48, 734, 290, 24, "Paciente", attendance.Name);
        page.Field(346, 734, 80, 24, "Idade", Value(attendance.Age));
        page.Field(434, 734, 112, 24, "Data", attendance.AttendanceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        page.TextArea(
            48,
            678,
            498,
            36,
            "Resumo",
            BuildTriageAssessmentSummary(attendance));

        page.Table(
            48,
            638,
            498,
            "VALORES, LIMITES E ALERTAS",
            ["Sinal", "Valor | Limite | Status"],
            BuildTriageAssessmentRows(attendance),
            minRows: 5,
            maxRows: 6);

        page.TextArea(
            48,
            170,
            498,
            48,
            "Observacao",
            "Parecer automatico de apoio a triagem. Nao substitui avaliacao clinica, protocolos locais, repeticao de medidas suspeitas ou decisao do profissional responsavel.");

        return page.ToString();
    }

    private static string BuildResponsiblesPage(MedicalAttendance attendance)
    {
        var page = new PdfPageBuilder();

        page.Rect(32, 34, 531, 774);
        page.Text(48, 785, "RESPONSAVEIS PELO ATENDIMENTO", 14, bold: true);
        page.Text(390, 785, $"Ficha: {attendance.Id}", 10, bold: true);
        page.Line(32, 772, 563, 772);

        page.Field(48, 734, 330, 24, "Paciente", attendance.Name);
        page.Field(386, 734, 160, 24, "Data", attendance.AttendanceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        page.Table(
            48,
            690,
            498,
            "ASSINATURAS POR ETAPA",
            ["Etapa", "Responsavel"],
            BuildResponsibleRows(attendance),
            minRows: 3,
            maxRows: 3);

        page.TextArea(
            48,
            466,
            498,
            60,
            "Observacao",
            "Cada etapa possui assinatura independente. Etapas sem assinatura permanecem marcadas como nao assinadas.");

        return page.ToString();
    }

    private static string BuildProceduresPage(MedicalAttendance attendance)
    {
        var page = new PdfPageBuilder();

        page.Rect(32, 34, 531, 774);
        page.Text(48, 785, "FICHA DE ATENDIMENTO MEDICO - VERSO", 14, bold: true);
        page.Text(390, 785, $"Ficha: {attendance.Id}", 10, bold: true);
        page.Line(32, 772, 563, 772);

        int y = 742;
        page.Table(
            48,
            y,
            498,
            "PRESCRICAO MEDICA",
            ["#", "Descricao"],
            attendance.Prescriptions
                .OrderBy(item => item.Order)
                .Select(item => new[] { item.Order.ToString(CultureInfo.InvariantCulture), PrescriptionDescription(item) })
                .ToArray(),
            minRows: 6,
            maxRows: 6);

        y = 470;
        page.Table(
            48,
            y,
            498,
            "CHECAGEM ENFERMAGEM",
            ["#", "Descricao"],
            attendance.NursingChecks
                .OrderBy(item => item.Order)
                .Select(item => new[] { item.Order.ToString(CultureInfo.InvariantCulture), Value(item.Description) })
                .ToArray(),
            minRows: 5,
            maxRows: 5);

        y = 244;
        page.Table(
            48,
            y,
            498,
            "DISPENSACAO FARMACIA",
            ["#", "Medicamento/material dispensado"],
            attendance.Dispensations
                .OrderBy(item => item.Order)
                .Select(item => new[] { item.Order.ToString(CultureInfo.InvariantCulture), DispensationDescription(item) })
                .ToArray(),
            minRows: 3,
            maxRows: 3);

        page.Field(48, 44, 498, 24, "Possui verso", attendance.HasBackSide ? "Sim" : "Nao");

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
            null => "-",
            DateTimeOffset dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("HH:mm", CultureInfo.InvariantCulture),
            decimal decimalValue => decimalValue.ToString("0.##", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "-"
        };
    }

    private static string DispensationDescription(MedicalAttendanceDispensationItem item)
    {
        if (!item.PrescriptionId.HasValue)
        {
            return Value(item.Batch);
        }

        string medication = Value(item.MedicationName);
        string quantity = Value(item.Quantity);
        string batch = Value(item.Batch);
        return $"{medication} | Qtde: {quantity} | Lote: {batch} | Presc.: {item.PrescriptionId}";
    }

    private static string Cid10Value(MedicalAttendance attendance)
    {
        if (string.IsNullOrWhiteSpace(attendance.Cid10Code))
        {
            return "-";
        }

        return string.IsNullOrWhiteSpace(attendance.Cid10Name)
            ? attendance.Cid10Code
            : $"{attendance.Cid10Code} - {attendance.Cid10Name}";
    }

    private static string PrescriptionDescription(MedicalAttendancePrescriptionItem item)
    {
        if (!item.MedicationId.HasValue)
        {
            return Value(item.Description);
        }

        string medication = Value(item.MedicationName);
        string dosage = Value(item.Dosage);
        string quantity = Value(item.Quantity);
        string directions = Value(item.Directions);

        return $"{medication} | {dosage} | Qtde: {quantity} | {directions}";
    }

    private static string[][] BuildResponsibleRows(MedicalAttendance attendance)
    {
        return
        [
            ["Triagem", ResponsibleDescription(
                attendance.TriageResponsibleUserId ?? attendance.ResponsibleUserId,
                attendance.TriageResponsibleName ?? attendance.ResponsibleName,
                attendance.TriageResponsibleSignature ?? attendance.ResponsibleSignature)],
            ["Medico", ResponsibleDescription(
                attendance.MedicalResponsibleUserId,
                attendance.MedicalResponsibleName,
                attendance.MedicalResponsibleSignature)],
            ["Farmacia", ResponsibleDescription(
                attendance.DispensationResponsibleUserId,
                attendance.DispensationResponsibleName,
                attendance.DispensationResponsibleSignature)]
        ];
    }

    private static string ResponsibleDescription(long? userId, string? name, string? signature)
    {
        if (!userId.HasValue && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(signature))
        {
            return "Nao assinado";
        }

        string responsible = string.IsNullOrWhiteSpace(name)
            ? Value(signature)
            : name.Trim();
        string signedAs = string.IsNullOrWhiteSpace(signature) || string.Equals(signature.Trim(), responsible, StringComparison.Ordinal)
            ? string.Empty
            : $" | Ass.: {signature.Trim()}";
        string id = userId.HasValue ? $" | Usuario: {userId.Value}" : string.Empty;

        return $"{responsible}{id}{signedAs}";
    }

    private static string[][] BuildTriageAssessmentRows(MedicalAttendance attendance)
    {
        List<string[]> rows = [];
        VitalSigns vitalSigns = attendance.VitalSigns;

        AddIfPresent(rows, AssessBloodPressure(attendance.Age, vitalSigns.SystolicPressure, vitalSigns.DiastolicPressure));
        AddIfPresent(rows, AssessHeartRate(attendance.Age, vitalSigns.HeartRate));
        AddIfPresent(rows, AssessTemperature(vitalSigns.Temperature));
        AddIfPresent(rows, AssessOxygenSaturation(vitalSigns.OxygenSaturation));
        AddIfPresent(rows, AssessBloodGlucose(vitalSigns.BloodGlucose));

        return rows.Count == 0
            ? [["-", "Sem sinais vitais suficientes para avaliacao de apoio."]]
            : rows.ToArray();
    }

    private static string BuildTriageAssessmentSummary(MedicalAttendance attendance)
    {
        string[][] rows = BuildTriageAssessmentRows(attendance);
        if (rows.Length == 1 && rows[0][0] == "-")
        {
            return "Sem dados suficientes para avaliar sinais vitais.";
        }

        int alerts = rows.Count(row => row[1].Contains("ALERTA", StringComparison.OrdinalIgnoreCase));
        int attention = rows.Count(row => row[1].Contains("ATENCAO", StringComparison.OrdinalIgnoreCase));

        if (alerts > 0)
        {
            return $"{alerts} sinal(is) em alerta. Confirmar medidas e priorizar avaliacao conforme protocolo.";
        }

        if (attention > 0)
        {
            return $"{attention} sinal(is) requerem atencao. Reavaliar, acompanhar evolucao e correlacionar sintomas.";
        }

        return "Sinais vitais informados dentro das faixas de apoio registradas.";
    }

    private static void AddIfPresent(List<string[]> rows, TriageAssessmentRow? row)
    {
        if (row is null)
        {
            return;
        }

        rows.Add([row.Label, $"{row.Value} | Limite: {row.Limit} | {row.Status} - {row.Message}"]);
    }

    private static TriageAssessmentRow? AssessBloodPressure(int? age, int? systolic, int? diastolic)
    {
        if (!systolic.HasValue || !diastolic.HasValue || systolic <= 0 || diastolic <= 0)
        {
            return null;
        }

        if (systolic < 40 || systolic > 260 || diastolic < 20 || diastolic > 160 || diastolic >= systolic)
        {
            return new TriageAssessmentRow(
                "PA",
                $"{systolic}/{diastolic} mmHg",
                "40-260/20-160 e diastolica < sistolica",
                "ALERTA",
                "valor inconsistente; conferir digitacao/equipamento");
        }

        BloodPressureRange range = BloodPressureRangeForAge(age);
        string limit = $"{range.Systolic.Min}-{range.Systolic.Max}/{range.Diastolic.Min}-{range.Diastolic.Max} mmHg";
        bool isAdult = !age.HasValue || age >= 18;

        if (systolic >= 180 || diastolic >= 120 || systolic < 80 || diastolic < 45)
        {
            return new TriageAssessmentRow("PA", $"{systolic}/{diastolic} mmHg", limit, "ALERTA", "limite critico de apoio");
        }

        if (systolic < range.Systolic.Min || diastolic < range.Diastolic.Min)
        {
            return new TriageAssessmentRow("PA", $"{systolic}/{diastolic} mmHg", limit, "ATENCAO", "abaixo da faixa de apoio");
        }

        if (isAdult && (systolic >= 140 || diastolic >= 90))
        {
            return new TriageAssessmentRow("PA", $"{systolic}/{diastolic} mmHg", limit, "ALERTA", "elevacao importante em adulto");
        }

        if (systolic > range.Systolic.Max || diastolic > range.Diastolic.Max)
        {
            bool pediatricAlert = !isAdult && (systolic > range.Systolic.Max + 20 || diastolic > range.Diastolic.Max + 15);
            return new TriageAssessmentRow(
                "PA",
                $"{systolic}/{diastolic} mmHg",
                limit,
                pediatricAlert ? "ALERTA" : "ATENCAO",
                "acima da faixa de apoio");
        }

        return new TriageAssessmentRow("PA", $"{systolic}/{diastolic} mmHg", limit, "OK", "dentro do limite");
    }

    private static TriageAssessmentRow? AssessHeartRate(int? age, int? heartRate)
    {
        if (!heartRate.HasValue || heartRate <= 0)
        {
            return null;
        }

        if (heartRate < 25 || heartRate > 260)
        {
            return new TriageAssessmentRow("FC", $"{heartRate} bpm", "25-260 bpm", "ALERTA", "valor inconsistente; repetir medida");
        }

        NumericRange range = HeartRateRangeForAge(age);
        string limit = $"{range.Min}-{range.Max} bpm";

        if (heartRate < range.Min || heartRate > range.Max)
        {
            string status = heartRate < range.Min - 15 || heartRate > range.Max + 20 ? "ALERTA" : "ATENCAO";
            return new TriageAssessmentRow("FC", $"{heartRate} bpm", limit, status, "fora da faixa de apoio");
        }

        return new TriageAssessmentRow("FC", $"{heartRate} bpm", limit, "OK", "dentro do limite");
    }

    private static TriageAssessmentRow? AssessTemperature(decimal? temperature)
    {
        if (!temperature.HasValue || temperature <= 0)
        {
            return null;
        }

        string value = $"{temperature.Value.ToString("0.0", CultureInfo.InvariantCulture)} C";
        if (temperature < 30 || temperature > 45)
        {
            return new TriageAssessmentRow("Temperatura", value, "30.0-45.0 C plausivel", "ALERTA", "valor inconsistente; repetir medida");
        }

        if (temperature < 35 || temperature >= 39)
        {
            return new TriageAssessmentRow("Temperatura", value, "35.0-37.7 C", "ALERTA", "fora de limite importante");
        }

        if (temperature >= 37.8m)
        {
            return new TriageAssessmentRow("Temperatura", value, "35.0-37.7 C", "ATENCAO", "febre/estado febril");
        }

        return new TriageAssessmentRow("Temperatura", value, "35.0-37.7 C", "OK", "dentro do limite");
    }

    private static TriageAssessmentRow? AssessOxygenSaturation(int? oxygenSaturation)
    {
        if (!oxygenSaturation.HasValue || oxygenSaturation <= 0)
        {
            return null;
        }

        if (oxygenSaturation > 100)
        {
            return new TriageAssessmentRow("SpO2", $"{oxygenSaturation}%", "0-100%", "ALERTA", "valor inconsistente");
        }

        if (oxygenSaturation < 92)
        {
            return new TriageAssessmentRow("SpO2", $"{oxygenSaturation}%", "95-100%", "ALERTA", "saturacao baixa");
        }

        if (oxygenSaturation < 95)
        {
            return new TriageAssessmentRow("SpO2", $"{oxygenSaturation}%", "95-100%", "ATENCAO", "abaixo do ideal");
        }

        return new TriageAssessmentRow("SpO2", $"{oxygenSaturation}%", "95-100%", "OK", "dentro do limite");
    }

    private static TriageAssessmentRow? AssessBloodGlucose(decimal? bloodGlucose)
    {
        if (!bloodGlucose.HasValue || bloodGlucose <= 0)
        {
            return null;
        }

        string value = $"{bloodGlucose.Value.ToString("0.##", CultureInfo.InvariantCulture)} mg/dL";
        if (bloodGlucose > 600)
        {
            return new TriageAssessmentRow("Glicemia", value, "70-140 mg/dL casual", "ALERTA", "muito alto ou possivel erro");
        }

        if (bloodGlucose < 54 || bloodGlucose >= 250)
        {
            return new TriageAssessmentRow("Glicemia", value, "70-140 mg/dL casual", "ALERTA", "fora de limite importante");
        }

        if (bloodGlucose < 70 || bloodGlucose > 140)
        {
            return new TriageAssessmentRow("Glicemia", value, "70-140 mg/dL casual", "ATENCAO", "fora da faixa de apoio");
        }

        return new TriageAssessmentRow("Glicemia", value, "70-140 mg/dL casual", "OK", "dentro do limite");
    }

    private static BloodPressureRange BloodPressureRangeForAge(int? age)
    {
        if (age is < 1)
        {
            return new BloodPressureRange(new NumericRange(72, 104), new NumericRange(37, 56));
        }

        if (age is <= 2)
        {
            return new BloodPressureRange(new NumericRange(86, 106), new NumericRange(42, 63));
        }

        if (age is <= 5)
        {
            return new BloodPressureRange(new NumericRange(89, 112), new NumericRange(46, 72));
        }

        if (age is <= 12)
        {
            return new BloodPressureRange(new NumericRange(97, 120), new NumericRange(57, 80));
        }

        if (age is <= 17)
        {
            return new BloodPressureRange(new NumericRange(110, 131), new NumericRange(64, 83));
        }

        return new BloodPressureRange(new NumericRange(90, 119), new NumericRange(60, 79));
    }

    private static NumericRange HeartRateRangeForAge(int? age)
    {
        if (age is < 1) return new NumericRange(100, 160);
        if (age is <= 2) return new NumericRange(90, 150);
        if (age is <= 5) return new NumericRange(80, 140);
        if (age is <= 12) return new NumericRange(70, 120);
        return new NumericRange(60, 100);
    }

    private sealed record TriageAssessmentRow(
        string Label,
        string Value,
        string Limit,
        string Status,
        string Message);

    private readonly record struct NumericRange(int Min, int Max);

    private readonly record struct BloodPressureRange(
        NumericRange Systolic,
        NumericRange Diastolic);

    private sealed class PdfPageBuilder
    {
        private readonly StringBuilder builder = new();

        public void Section(int x, int y, string title)
        {
            Rect(x, y, 498, 18, fillGray: "0.92");
            Text(x + 8, y + 6, title, 10, bold: true);
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

        public void Table(
            int x,
            int y,
            int width,
            string title,
            IReadOnlyList<string> headers,
            IReadOnlyList<string[]> rows,
            int minRows,
            int? maxRows = null)
        {
            Section(x, y, title);

            int top = y - 44;
            int rowHeight = 34;
            int firstColumnWidth = 42;
            int bodyWidth = width - firstColumnWidth;
            int totalRows = Math.Max(minRows, rows.Count);
            if (maxRows.HasValue)
            {
                totalRows = Math.Min(totalRows, maxRows.Value);
            }

            int visibleDataRows = Math.Min(rows.Count, totalRows);
            bool hasHiddenRows = rows.Count > totalRows;
            if (hasHiddenRows && visibleDataRows > 0)
            {
                visibleDataRows--;
            }

            Rect(x, top, width, rowHeight, fillGray: "0.96");
            Line(x + firstColumnWidth, top, x + firstColumnWidth, top + rowHeight);
            Text(x + 8, top + 13, headers[0], 8, bold: true);
            Text(x + firstColumnWidth + 8, top + 13, headers[1], 8, bold: true);

            for (int index = 0; index < totalRows; index++)
            {
                int rowY = top - ((index + 1) * rowHeight);
                Rect(x, rowY, width, rowHeight);
                Line(x + firstColumnWidth, rowY, x + firstColumnWidth, rowY + rowHeight);

                if (index < visibleDataRows)
                {
                    string[] row = rows[index];
                    TextWrapped(x + 8, rowY + rowHeight - 12, firstColumnWidth - 12, rowHeight - 8, row[0], 7, maxLines: 3);
                    TextWrapped(
                        x + firstColumnWidth + 8,
                        rowY + rowHeight - 12,
                        bodyWidth - 12,
                        rowHeight - 8,
                        row[1],
                        7,
                        maxLines: 3);
                }
                else if (hasHiddenRows && index == visibleDataRows)
                {
                    int hiddenCount = rows.Count - visibleDataRows;
                    Text(x + 8, rowY + 13, "...", 8);
                    TextWrapped(
                        x + firstColumnWidth + 8,
                        rowY + rowHeight - 12,
                        bodyWidth - 12,
                        rowHeight - 8,
                        $"+ {hiddenCount} item(ns) adicional(is) nao exibido(s) nesta pagina.",
                        7,
                        maxLines: 3);
                }
            }
        }

        private void TextWrapped(int x, int y, int width, int height, string value, int size, int maxLines)
        {
            int maxChars = Math.Max(8, width / Math.Max(4, size / 2));
            int availableLines = Math.Max(1, Math.Min(maxLines, height / (size + 3)));
            int lineY = y;

            foreach (string line in Wrap(value, maxChars).Take(availableLines))
            {
                Text(x, lineY, line, size);
                lineY -= size + 3;
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
