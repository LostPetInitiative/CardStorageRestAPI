using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PatCardStorageAPI.Storage;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using CardStorageRestAPI;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PatCardStorageAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PetPhotosController : ControllerBase
    {
        private IPhotoStorage storage;
        public PetPhotosController(IPhotoStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Returns the image identified by the order nnum <paramref name="imNum"/> in the pet card <paramref name="localID"/> of the card namespace <paramref name="ns"/>
        /// If the <paramref name="preferableProcessings"/> are specified. The processed image is returned.
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="localID"></param>
        /// <param name="imNum"></param>
        /// <param name="preferableProcessings">In the order of preference. comma separated identifications</param>
        /// <returns></returns>
        [EnableCors]
        [HttpGet("{nsStr}/{localIDstr}/{imNum}")]
        public async Task<IActionResult> GetImage(string nsStr, string localIDstr, int imNum, [FromQuery]string? preferableProcessingsStr) {
            try
            {
                // verifying inputs
                AsciiIdentifier ns = new(nsStr);
                AsciiIdentifier localID = new(localIDstr);

                try
                {
                    Trace.TraceInformation($"Getting raw photo #{imNum} for {ns}/{localID}");
                    var photo = await this.storage.GetOriginalPhotoAsync(ns, localID, imNum);
                    if (photo == null)
                    {
                        Trace.TraceInformation($"photo #{imNum} for {ns}/{localID} does not exist. Returning not found");
                        return NotFound();
                    }
                    else
                    {                    
                        Trace.TraceInformation($"Extracted photo #{imNum} for {ns}/{localID} from storage. uuid: {photo.Uuid}");
                        string? mimeType = null;
                        byte[]? imageContent = null;                    
                        if(!string.IsNullOrEmpty(preferableProcessingsStr))
                        {

                            foreach (var processingIdent in preferableProcessingsStr.Split(',').Select(s => new AsciiIdentifier(s)))
                            {
                                Trace.TraceInformation($"Checking existence of {processingIdent} for {photo.Uuid}");
                                var processedPhoto = await this.storage.GetProcessedPetPhotoAsync(photo.Uuid, processingIdent);
                                if (processedPhoto != null) {
                                    Trace.TraceInformation($"Transmitting processed image ({processingIdent}) to client according to the client preferences");
                                    mimeType = processedPhoto.ImageMimeType ?? "image";
                                    imageContent = processedPhoto.Image;
                                    break;
                                }
                            }
                        }

                        if(imageContent == null) {
                            Trace.TraceInformation($"Transmitting unprocessed image to client");
                            mimeType = photo.ImageMimeType ?? "image";
                            imageContent = photo.Image;
                        }

                    
                        return File(imageContent, mimeType);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Exception during retrieving raw photo {imNum} for {ns}/{localID}: {ex}");
                    return StatusCode(500, ex.ToString());
                }
            }
            catch (ArgumentException ae)
            {
                Trace.TraceError(ae.ToString());
                return BadRequest(ae);
            }
        }

        // GET: <PetPhotoController>/pet911ru/rf123
        [HttpGet("{nsStr}/{localIDstr}")]
        [EnableCors]
        public async IAsyncEnumerable<JsonPoco.PhotoDescriptorOut> GetAll(string nsStr, string localIDstr, [FromQuery] string? featuresToInclude)
        {
            // verifying inputs
            AsciiIdentifier ns = new(nsStr);
            AsciiIdentifier localID = new(localIDstr);

            string[] featuresToIncludeSplit = new string[0];
            if (featuresToInclude != null) {
                featuresToIncludeSplit = featuresToInclude.Trim().Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            }

            Trace.TraceInformation($"Getting photos for {ns}/{localID}");
            await foreach (var photo in this.storage.ListOriginalPhotosAsync(ns, localID))
            {
                if (photo == null) {
                    Trace.TraceInformation($"There are no photos for {ns}/{localID}");
                    // TODO: add NotFound exception here.
                    yield break;
                    throw new KeyNotFoundException($"There are no photos for {ns}/{localID}"); // unreachable code?
                }
                Trace.TraceInformation($"Yielding photo {photo.ImageNum} for {ns}/{localID}");

                var featuresTasks = featuresToIncludeSplit.Select(featuresIdent => this.storage.GetPhotoFeatures(photo.Uuid, new AsciiIdentifier(featuresIdent))).ToArray();

                Dictionary<string, double[]> featuresDict = new Dictionary<string, double[]>();
                for (int i = 0; i < featuresToIncludeSplit.Length; i++) {
                    var features = await featuresTasks[i];
                    if (features != null) featuresDict.Add(featuresToIncludeSplit[i], features);
                }
                yield return new JsonPoco.PhotoDescriptorOut(photo.ImageNum, photo.Uuid, featuresDict);
            }
        }
        
        [HttpPut("{nsStr}/{localIDstr}/{imNum}")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Put(string nsStr, string localIDstr, int imNum, [FromBody ]JsonPoco.PetPhoto photo)
        {
            try
            {
                // verifying inputs
                AsciiIdentifier ns = new(nsStr);
                AsciiIdentifier localID = new(localIDstr);

                try
                {
                    Trace.TraceInformation($"Adding photo {imNum} for {ns}/{localID}");
                    (Guid uuid, bool created) = await this.storage.AddOriginalPetPhotoAsync(ns, localID, imNum, photo.ToStoragePetPhoto());
                    if(created)
                    {
                        Trace.TraceInformation($"successfully added photo {imNum} for {ns}/{localID}. UUID: {uuid}");
                        var routeValues = new { nsStr = ns.ToString(), localIDstr = localID.ToString(), imNum = imNum };
                        return CreatedAtAction(nameof(GetImage), routeValues, uuid);
                    }
                    else
                    {
                        Trace.TraceError($"Photo {imNum} for {ns}/{localID} already exists");                    
                        return new ConflictObjectResult(uuid);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Exception during adding a photo {imNum} for {ns}/{localID}: {ex}");
                    return StatusCode(StatusCodes.Status500InternalServerError, ex.ToString());
                }
            }
            catch (ArgumentException ae)
            {
                Trace.TraceError(ae.ToString());
                return BadRequest(ae);
            }
        }

        [HttpPut("{nsStr}/{localIDstr}/{imNum}/processed/{processingIdentStr}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Put(string nsStr, string localIDstr, int imNum,string processingIdentStr, [FromBody] JsonPoco.PetPhoto photo)
        {
            try
            {
                // verifying inputs
                AsciiIdentifier ns = new(nsStr);
                AsciiIdentifier localID = new(localIDstr);
                AsciiIdentifier processingIdent = new(processingIdentStr);

                try
                {
                    Trace.TraceInformation($"Adding processed ({processingIdent}) photo {imNum} for {ns}/{localID}");

                    // TODO: guard against injections here
                    var orig = await storage.GetOriginalPhotoAsync(ns, localID, imNum);
                    if (orig == null) {
                        return NotFound($"Original photo identified by {ns}/{localID}/{imNum} is not found");
                    }                

                    bool created = await this.storage.AddProcessedPetPhotoAsync(orig.Uuid, processingIdent, photo.ToStoragePetPhoto());
                    if (created)
                    {
                        Trace.TraceInformation($"successfully added processed ({processingIdent}) photo {imNum} for {ns}/{localID}. UUID: {orig.Uuid}");
                        var routeValues = new { nsStr = ns.ToString(), localIDstr = localID.ToString(), imNum = imNum, preferableProcessingsStr = processingIdent.ToString() };
                        return CreatedAtAction(nameof(GetImage), routeValues, orig.Uuid);
                    }
                    else
                    {
                        Trace.TraceError($"Processed ({processingIdent}) photo {imNum} for {ns}/{localID}. UUID: {orig.Uuid} already exists");
                        return new ConflictResult();
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Exception during adding a photo {imNum} for {ns}/{localID}: {ex}");
                    return StatusCode(StatusCodes.Status500InternalServerError, ex.ToString());
                }
            }
            catch (ArgumentException ae)
            {
                Trace.TraceError(ae.ToString());
                return BadRequest(ae);
            }
        }

        [HttpPut("{nsStr}/{localIDstr}/{imNum}/features/{featuresIdentStr}")]
        public async Task<IActionResult> PutFeatures(string nsStr, string localIDstr, int imNum, string featuresIdentStr, [FromBody] JsonPoco.FeaturesPOCO features)
        {
            try
            {
                // verifying inputs
                AsciiIdentifier ns = new(nsStr);
                AsciiIdentifier localID = new(localIDstr);
                AsciiIdentifier featuresIdent = new(featuresIdentStr);

                try
                {
                    // TODO: guard against injections here
                    var orig = await storage.GetOriginalPhotoAsync(ns, localID, imNum);
                    if (orig == null)
                    {
                        return NotFound($"Original photo identified by {ns}/{localID}/{imNum} is not found");
                    }

                    Trace.TraceInformation($"Setting features {featuresIdent} for {ns}/{localID}/{imNum} ({orig.Uuid})");
                    await this.storage.SetPhotoFeatureVectorAsync(orig.Uuid, featuresIdent, features.Features);
                    Trace.TraceInformation($"Successfully set features {featuresIdent} for {ns}/{localID}/{imNum} ({orig.Uuid})");
                    return Ok();
                }
                catch (Exception err)
                {
                    Trace.TraceError($"Except card for {ns}/{localID}: {err}");
                    return StatusCode(500, err.ToString());
                }
            }
            catch (ArgumentException ae)
            {
                Trace.TraceError(ae.ToString());
                return BadRequest(ae);
            }
        }

        [HttpDelete("{nsStr}/{localIDstr}/{photoNum?}")]
        public async Task<ActionResult<bool>> Delete(string nsStr, string localIDstr, int photoNum)
        {
            try
            {
                // verifying inputs
                AsciiIdentifier ns = new(nsStr);
                AsciiIdentifier localID = new(localIDstr);

                try
                {
                    if (photoNum == 0)
                    {
                        // parameter is omitted. thus passing -1 to the storage
                        photoNum = -1;
                    }

                    Trace.TraceInformation($"Deleting photo {photoNum} for {ns}/{localID}");
                    bool res = await this.storage.DeleteOriginalPetPhoto(ns, localID, photoNum);
                    if (res)
                        Trace.TraceInformation($"Successfully deleted photo {photoNum} for {ns}/{localID}");
                    else
                        Trace.TraceWarning($"Failed to delete photo {photoNum} for {ns}/{localID}");
                    return res;
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Exception during deleting a photo {photoNum} for {ns}/{localID}: {ex}");
                    return StatusCode(500, ex.ToString());
                }
            }
            catch (ArgumentException ae)
            {
                Trace.TraceError(ae.ToString());
                return BadRequest(ae);
            }
        }
    }
}
