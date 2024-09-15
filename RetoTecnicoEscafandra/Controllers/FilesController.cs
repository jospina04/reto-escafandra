using Microsoft.AspNetCore.Mvc;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;

namespace RetoTecnicoEscafandra.Controllers
{
    public class FilesController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public IActionResult Index()
        {
            return View();
        }

        public FilesController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost, Route("upload")]

        public ActionResult uploadFile([FromForm] IFormFile file)
        {
            try
            {
                string finalRoute = _webHostEnvironment.ContentRootPath + "\\UploadFiles";
                if (!Directory.Exists(finalRoute)) Directory.CreateDirectory(finalRoute);
                string completeFinalRoute = Path.Combine(finalRoute, file.FileName);

                if (file.Length > 0)
                {
                    using (var stream = new FileStream(completeFinalRoute, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }
                }

                return Ok("PDF File for Escafandra Upload");
            }
            catch (Exception)
            {
                return BadRequest("[ERROR000] - Uploading PDF File");
            }
        }

        [HttpGet("download/{id}")]

        public ActionResult downloadDocument([FromRoute] string id)
        {
            try
            {
                string finalRoute = _webHostEnvironment.ContentRootPath + "\\UploadFiles";
                string completeFinalRoute = Path.Combine(finalRoute, id);
                byte[] bytes = System.IO.File.ReadAllBytes(completeFinalRoute);

                return File(bytes, "application/octet-stream", id);
            }
            catch (Exception)
            {
                return NotFound("[ERROR001] - The PDF does not exist in Escafandra System]");
            }
        }

        [HttpPost("sign/{id}")]
        public ActionResult signDocument([FromRoute] string id)
        {
            try
            {
                // Ruta donde se almacenan los archivos
                string finalRoute = _webHostEnvironment.ContentRootPath + "\\UploadFiles";
                string completeFinalRoute = Path.Combine(finalRoute, id);

                // Verificar si el archivo existe
                if (!System.IO.File.Exists(completeFinalRoute))
                {
                    return NotFound("[ERROR002] - The PDF does not exist in Escafandra System");
                }

                // Aquí comienza el proceso de firma
                byte[] signedPdf = SignPdf(completeFinalRoute);

                // Guardar el archivo firmado
                string signedFileName = Path.Combine(finalRoute, $"signed_{id}");
                System.IO.File.WriteAllBytes(signedFileName, signedPdf);

                // Retornar el archivo firmado como respuesta
                return File(signedPdf, "application/pdf", $"signed_{id}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"[ERROR003] - An error occurred while signing the PDF: {ex.Message}");
            }
        }

        private byte[] SignPdf(string pdfPath)
        {
            string certificatePath = _webHostEnvironment.ContentRootPath + "\\certificates\\myCertificate.pfx";
            string certPassword = "000";

            X509Certificate2 cert = new X509Certificate2(certificatePath, certPassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            Org.BouncyCastle.X509.X509Certificate bcCert = DotNetUtilities.FromX509Certificate(cert);

            AsymmetricKeyParameter privateKey = DotNetUtilities.GetKeyPair(cert.PrivateKey).Private;

            using (Stream pdfStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read))
            using (MemoryStream signedPdfStream = new MemoryStream())
            {
                PdfReader pdfReader = new PdfReader(pdfStream);

                PdfStamper stamper = PdfStamper.CreateSignature(pdfReader, signedPdfStream, '\0');

                IExternalSignature externalSignature = new PrivateKeySignature(privateKey, DigestAlgorithms.SHA256);

                Org.BouncyCastle.X509.X509Certificate[] certChain = new Org.BouncyCastle.X509.X509Certificate[] { bcCert };

                MakeSignature.SignDetached(stamper.SignatureAppearance, externalSignature, certChain, null, null, null, 0, CryptoStandard.CMS);

                return signedPdfStream.ToArray();
            }
        }


    }
}
