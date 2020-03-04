﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Sistrategia.SAT.CFDiWebSite.CFDI;
using Sistrategia.SAT.CFDiWebSite.CloudStorage;
using Sistrategia.SAT.CFDiWebSite.Models;

namespace Sistrategia.SAT.CFDiWebSite.Controllers
{
    //[Authorize]
    public class DevController : BaseController
    {
        // GET: Dev
        public ActionResult Index()
        {
            this.ViewBag.cfdiService = ConfigurationManager.AppSettings["cfdiService"];
            this.ViewBag.cfdiServiceTimeSpan = SATManager.GetCFDIServiceTimeSpan().Minutes.ToString();

            return View();
        }


        public ActionResult GetTimbradoLog()
        {

            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureDefaultStorageConnectionString"]);
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(ConfigurationManager.AppSettings["AzureDefaultStorage"]);

            var list = container.ListBlobs();
            var itemList = new List<CloudStorageItem>();

            var readPolicy = new Microsoft.WindowsAzure.Storage.Blob.SharedAccessBlobPolicy()
            {
                Permissions = Microsoft.WindowsAzure.Storage.Blob.SharedAccessBlobPermissions.Read, // SharedAccessPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow + TimeSpan.FromMinutes(10)
            };

            foreach (var blob in list.OfType<Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob>().OrderByDescending(x => x.Name))
            {
                blob.FetchAttributes();
                var item = new CloudStorageItem
                {
                    Name = blob.Name,
                    Url = new Uri(blob.Uri.AbsoluteUri + blob.GetSharedAccessSignature(readPolicy)).ToString(), //  blob.Uri.AbsolutePath
                    ContentMD5 = blob.Properties.ContentMD5
                };
                itemList.Add(item);
            }

            var model = new GetTimbradoLogViewModel
            {
                CloudStorageItems = itemList
            };

            return View(model);
        }

        public ActionResult DatabaseSeed()
        {
            var config = new Migrations.Configuration();
            config.ReSeed(this.DBContext);
            return RedirectToAction("Index");
        }

        public ActionResult TestCFDI33()
        {
            var comprobante = new CFDI.Comprobante("3.3");

            comprobante.DecimalFormat = "0.00";
            comprobante.TipoDeComprobante = "I";

            comprobante.Serie = "A";
            comprobante.Folio = "167";
            comprobante.Fecha = DateTime.Parse("2018-01-03T16:57:04");

            CFDI.Certificado certificado = DBContext.Certificados.Find(3);

            if (certificado != null)
            {
                comprobante.CertificadoId = certificado.CertificadoId;
                comprobante.Certificado = certificado;
                comprobante.HasNoCertificado = true;
                comprobante.HasCertificado = true;
            }

            comprobante.FormaPago = "01";
            comprobante.MetodoPago = "PUE";
            comprobante.Moneda = "MXN";
            comprobante.TipoDeComprobante = "I";
            comprobante.CondicionesDePago = "CONDICIONES";
            comprobante.TipoCambio = "1";
            comprobante.LugarExpedicion = "45079";
            comprobante.SubTotal = 100m;
            comprobante.Total = 116m;

            CFDI.Receptor receptor = DBContext.Receptores.Find(54);
            receptor.UsoCFDI = "P01";

            CFDI.ComprobanteReceptor comprobanteReceptor = DBContext.ComprobantesReceptores.Find(51);

            // Crear uno nuevo
            if (comprobanteReceptor == null)
            {
                comprobanteReceptor = new CFDI.ComprobanteReceptor
                {
                    Receptor = receptor
                };
            }

            comprobante.Receptor = comprobanteReceptor;

            CFDI.Emisor emisor = DBContext.Emisores.Find(1);
            CFDI.ComprobanteEmisor comprobanteEmisor = null;
            comprobanteEmisor = DBContext.ComprobantesEmisores.Find(1);

            List <CFDI.ComprobanteEmisorRegimenFiscal> regimenes = new List<CFDI.ComprobanteEmisorRegimenFiscal>();

            regimenes.Add(new CFDI.ComprobanteEmisorRegimenFiscal()
            {
                RegimenFiscal = new CFDI.RegimenFiscal()
                {
                    RegimenFiscalClave = "601",
                    //Regimen = "601",
                }
            });

            if (comprobanteEmisor == null)
            {
                comprobanteEmisor = new CFDI.ComprobanteEmisor
                {
                    Emisor = emisor,
                    DomicilioFiscal = emisor.DomicilioFiscal,
                    RegimenFiscal = regimenes
                };

            }
            comprobanteEmisor.Emisor.RegimenFiscal[0].RegimenFiscalClave = "601";
            comprobante.Emisor = comprobanteEmisor;


            comprobante.Conceptos = new List<CFDI.Concepto>();

            CFDI.Concepto concepto = new CFDI.Concepto
            {
                Cantidad = 10m,
                Unidad = "NA",
                NoIdentificacion = "NA",
                Descripcion = "PRODUCTO",
                ValorUnitario = 10m,
                Importe = 100m,
                PublicKey = Guid.NewGuid(),
                Ordinal = 1,
                ClaveProdServ = "01010101",
                ClaveUnidad = "F52",
            };

            concepto.Impuestos = new CFDI.ConceptoImpuestos();
            concepto.Impuestos.Traslados = new List<CFDI.ConceptoImpuestosTraslado>();

            concepto.Impuestos.Traslados.Add(new CFDI.ConceptoImpuestosTraslado
            {
                Base = 100m,
                TipoFactor = "Tasa",
                TasaOCuota = 0.160000m,
                Importe = 16m,
                Ordinal = 1,
                Impuesto = "002"
            });

            comprobante.Conceptos.Add(concepto);


            comprobante.Impuestos = new CFDI.Impuestos();
            comprobante.Impuestos.Traslados = new List<CFDI.Traslado>();


            comprobante.Impuestos.Traslados.Add(new CFDI.Traslado
            {
                Importe = 16m,
                Impuesto = "002",
                TasaOCuota = 0.16m,
                TipoFactor = "Tasa"
            });

            comprobante.Impuestos.TotalImpuestosTrasladados = 16m;

            string cadenaOriginal = comprobante.GetCadenaOriginal();
            comprobante.Sello = certificado.GetSello(cadenaOriginal);

            DBContext.Comprobantes.Add(comprobante);
            DBContext.SaveChanges();

            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            CFDI.CFDIXmlTextWriter writer =
                new CFDI.CFDIXmlTextWriter(comprobante, /*ms*/Response.OutputStream, System.Text.Encoding.UTF8);
            writer.WriteXml();
            ms.Position = 0;
            writer.Close();

            return File(ms, "text/xml");
        }

        public ActionResult ComplementoPago()
        {
            var comprobante = new CFDI.Comprobante("3.3");

            comprobante.DecimalFormat = "0.00";
            comprobante.TipoDeComprobante = "P";

            comprobante.Serie = "CP";
            comprobante.Folio = "23";
            comprobante.Fecha = DateTime.Now + SATManager.GetCFDIServiceTimeSpan();

            CFDI.Certificado certificado = DBContext.Certificados.Find(1);

            if (certificado != null)
            {
                comprobante.CertificadoId = certificado.CertificadoId;
                comprobante.Certificado = certificado;
                comprobante.HasNoCertificado = true;
                comprobante.HasCertificado = true;
            }


            comprobante.Moneda = "XXX";
            comprobante.LugarExpedicion = "62130";
            comprobante.SubTotal = 0m;
            comprobante.Total = 0m;

            CFDI.Receptor receptor = new Receptor();//DBContext.Receptores.Find(23);
            receptor.UsoCFDI = "P01";
            receptor.Nombre = "MAPED SILCO SA DE CV";
            receptor.RFC = "MSI0108101J7";

            CFDI.ComprobanteReceptor comprobanteReceptor = DBContext.ComprobantesReceptores.Find(23);

            // Crear uno nuevo
            if (comprobanteReceptor == null)
            {
                comprobanteReceptor = new CFDI.ComprobanteReceptor
                {
                    Receptor = receptor
                };
            }

            comprobante.Receptor = comprobanteReceptor;

            CFDI.Emisor emisor = new Emisor();//DBContext.Emisores.Find(1);
            emisor.RFC = "JEO110617QB7";
            emisor.Nombre = "JEOCSI SA DE CV";
            emisor.PublicKey = Guid.NewGuid();
            CFDI.ComprobanteEmisor comprobanteEmisor = null;
            //comprobanteEmisor = DBContext.ComprobantesEmisores.Find(1);

            List<CFDI.ComprobanteEmisorRegimenFiscal> regimenes = new List<CFDI.ComprobanteEmisorRegimenFiscal>();

            regimenes.Add(new CFDI.ComprobanteEmisorRegimenFiscal()
            {
                RegimenFiscal = new CFDI.RegimenFiscal()
                {
                    RegimenFiscalClave = "601",
                    //Regimen = "601",
                }
            });

            if (comprobanteEmisor == null)
            {
                comprobanteEmisor = new CFDI.ComprobanteEmisor
                {
                    Emisor = emisor,
                    DomicilioFiscal = emisor.DomicilioFiscal,
                    RegimenFiscal = regimenes
                };
                comprobanteEmisor.Emisor.RegimenFiscal = new List<RegimenFiscal>();
                comprobanteEmisor.Emisor.RegimenFiscal.Add(new RegimenFiscal { RegimenFiscalClave = "601" });

            }
            //comprobanteEmisor.Emisor.RegimenFiscal[0].RegimenFiscalClave = "601";
            comprobante.Emisor = comprobanteEmisor;


            comprobante.Conceptos = new List<CFDI.Concepto>();

            CFDI.Concepto concepto = new CFDI.Concepto
            {
                Cantidad = 1m,
                //Unidad = "NA",
                //NoIdentificacion = "NA",
                Descripcion = "Pago",
                ValorUnitario = 0m,
                Importe = 0m,
                PublicKey = Guid.NewGuid(),
                Ordinal = 1,
                ClaveProdServ = "84111506",
                ClaveUnidad = "ACT",
            };

            //concepto.Impuestos = new CFDI.ConceptoImpuestos();
            //concepto.Impuestos.Traslados = new List<CFDI.ConceptoImpuestosTraslado>();

            //concepto.Impuestos.Traslados.Add(new CFDI.ConceptoImpuestosTraslado
            //{
            //    Base = 100m,
            //    TipoFactor = "Tasa",
            //    TasaOCuota = 0.160000m,
            //    Importe = 16m,
            //    Ordinal = 1,
            //    Impuesto = "002"
            //});

            comprobante.Conceptos.Add(concepto);

            ComprobantePago comprobantePago = new ComprobantePago();
            comprobantePago.Version = "1.0";
            comprobantePago.FechaPago = DateTime.Parse("2020-02-13T12:00:00");
            comprobantePago.FormaDePagoP = "03";
            comprobantePago.MonedaP = "MXN";
            comprobantePago.Monto = 13862.00m;
            comprobantePago.Ordinal = 1;

            //if (!string.IsNullOrEmpty(pago.NumeroOperacion))
            //    comprobantePago.NumOperacion = pago.NumeroOperacion;
            //if (!string.IsNullOrEmpty(pago.RfcEmisorCuentaOrigen))
            //    comprobantePago.RfcEmisorCtaOrd = pago.RfcEmisorCuentaOrigen;
            //if (!string.IsNullOrEmpty(pago.NombreBanco))
            //    comprobantePago.NombBancoOrdExt = pago.NombreBanco;
            //if (!string.IsNullOrEmpty(pago.CuentaOrdenante))
            //    comprobantePago.CtaOrdenante = pago.CuentaOrdenante;
            //if (!string.IsNullOrEmpty(pago.RfcEmisorCuentaBeneficiario))
            //    comprobantePago.RfcEmisorCtaBen = pago.RfcEmisorCuentaBeneficiario;
            //if (!string.IsNullOrEmpty(pago.CuentaBeneficiario))
            //    comprobantePago.CtaBeneficiario = pago.CuentaBeneficiario;
            //if (!string.IsNullOrEmpty(pago.TipoCadenaDePago))
            //    comprobantePago.TipoCadPago = pago.TipoCadenaDePago;
            //if (!string.IsNullOrEmpty(pago.CertificadoPago))
            //    comprobantePago.CertPago = pago.CertificadoPago;
            //if (!string.IsNullOrEmpty(pago.CadenaDePago))
            //    comprobantePago.CadPago = pago.CadenaDePago;
            //if (!string.IsNullOrEmpty(pago.SelloDePago))
            //    comprobantePago.SelloPago = pago.SelloDePago;

            ComprobantePagoDoctoRelacionado docto = new ComprobantePagoDoctoRelacionado();
            docto.ComprobantePagoDoctoRelacionadoId = Guid.NewGuid();
            docto.IdDocumento = "73384254-0873-4A22-9542-25D3CB1A717B";
            docto.Serie = "A";
            docto.Folio = "640";
            docto.MonedaDR = "MXN";
            docto.MetodoDePagoDR = "PPD";
            docto.NumParcialidades = 1;
            docto.ImpSaldAnt = 13862.00m;
            docto.ImpPagado = 13862.00m;
            docto.ImpSaldoInsoluto = 0.00m;
            docto.Ordinal = 1;

            comprobantePago.DoctosRelacionados.Add(docto);

            //ComprobantePagoDoctoRelacionado docto2 = new ComprobantePagoDoctoRelacionado();
            //docto2.ComprobantePagoDoctoRelacionadoId = Guid.NewGuid();
            //docto2.IdDocumento = "48AE6789-3948-4E6F-9613-10F112023B95";
            //docto2.Serie = "A";
            //docto2.Folio = "513";
            //docto2.MonedaDR = "MXN";
            //docto2.MetodoDePagoDR = "PPD";
            //docto2.NumParcialidades = 1;
            //docto2.ImpSaldAnt = 12760.00m;
            //docto2.ImpPagado = 12760.00m;
            //docto2.ImpSaldoInsoluto = 0m;
            //docto2.Ordinal = 2;

            //comprobantePago.DoctosRelacionados.Add(docto2);

            if (comprobante.Complementos == null)
                comprobante.Complementos = new List<Complemento>();
            comprobante.Complementos.Add(comprobantePago);

            //comprobante.Impuestos = new CFDI.Impuestos();
            //comprobante.Impuestos.Traslados = new List<CFDI.Traslado>();


            //comprobante.Impuestos.Traslados.Add(new CFDI.Traslado
            //{
            //    Importe = 16m,
            //    Impuesto = "002",
            //    TasaOCuota = 0.16m,
            //    TipoFactor = "Tasa"
            //});

            //comprobante.Impuestos.TotalImpuestosTrasladados = 16m;

            string cadenaOriginal = comprobante.GetCadenaOriginal();
            comprobante.Sello = certificado.GetSello(cadenaOriginal);

            string user = "JEO110617QB7";//ConfigurationManager.AppSettings["CfdiServiceUser"];
            string password = "crvthtbdo";// ConfigurationManager.AppSettings["CfdiServicePassword"];

            string invoiceFileName = DateTime.Now.ToString("yyyyMMddHHmmss_" + comprobante.PublicKey.ToString("N"));

            try {
                SATManager manager = new SATManager();
                bool response = manager.GetCFDI(user, password, comprobante);
            }
            catch (Exception ex) {
                //ex.Message;
            }


            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            CFDI.CFDIXmlTextWriter writer =
                new CFDI.CFDIXmlTextWriter(comprobante, /*ms*/Response.OutputStream, System.Text.Encoding.UTF8);
            writer.WriteXml();
            ms.Position = 0;
            writer.Close();

            return File(ms, "text/xml");

            //string user = ConfigurationManager.AppSettings["CfdiServiceUser"];
            //string password = ConfigurationManager.AppSettings["CfdiServicePassword"];

            //var model = new ComprobanteDetailViewModel(comprobante);

            //string invoiceFileName = DateTime.Now.ToString("yyyyMMddHHmmss_" + comprobante.PublicKey.ToString("N"));

            //try
            //{
            //    SATManager manager = new SATManager();
            //    bool response = manager.GetCFDI(user, password, comprobante);
            //    //if (response)
            //    //    DBContext.SaveChanges();

            //    return "Ok";
            //}
            //catch (Exception ex)
            //{
            //    return ex.Message.ToString();
            //}

        }

        public string Timbre33()
        {
            var comprobante = DBContext.Comprobantes.Find(2408);

            var certificado = DBContext.Certificados.Where(e => e.NumSerie == comprobante.NoCertificado).SingleOrDefault();

            string user = "JEO110617QB7";//ConfigurationManager.AppSettings["CfdiServiceUser"];
            string password = "crvthtbdo";// ConfigurationManager.AppSettings["CfdiServicePassword"];

            var model = new ComprobanteDetailViewModel(comprobante);

            string invoiceFileName = DateTime.Now.ToString("yyyyMMddHHmmss_" + comprobante.PublicKey.ToString("N"));

            try { 
                SATManager manager = new SATManager();
                bool response = manager.GetCFDI(user, password, comprobante);
                //if (response)
                //    DBContext.SaveChanges();

                return "Ok";
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
        }
    }
}