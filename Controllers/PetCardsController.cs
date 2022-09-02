using System;
using System.Collections.Generic;
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
    public class PetCardsController : ControllerBase
    {
        private ICardStorage storage;
        public PetCardsController(ICardStorage storage)
        {
            this.storage = storage;
        }

        // GET <PetCardsController>/pet911ru/rf123
        [EnableCors]
        [HttpGet("{nsStr}/{localIDstr}")]
        public async Task<ActionResult<PetCard>> Get(string nsStr, string localIDstr, [FromQuery] bool includeSensitiveData = false)
        {
            try
            {
                // verifying inputs
                AsciiIdentifier ns = new(nsStr);
                AsciiIdentifier localID = new(localIDstr);

                try
                {

                    Trace.TraceInformation($"Getting card for {ns}/{localID}");
                    var result = await this.storage.GetPetCardAsync(ns, localID);
                    if (result == null)
                    {
                        Trace.TraceInformation($"card for {ns}/{localID} not found");
                        return NotFound();
                    }
                    else
                    {
                        Trace.TraceInformation($"Successfully retrieved card for {ns}/{localID}");
                        if (!includeSensitiveData)
                        {
                            Trace.TraceInformation($"Wiping out sensitive data for {ns}/{localID}");
                            var contacts = result.ContactInfo;
                            if (contacts != null)
                            {
                                contacts.Email = new string[0];
                                contacts.Name = "";
                                contacts.Tel = new string[0];
                                contacts.Website = new string[0];
                            }
                        }
                        else
                        {
                            Trace.TraceInformation($"Returning sensitive data to the client for {ns}/{localID}");
                        }
                        return new ActionResult<PetCard>(result);
                    }
                }
                catch (Exception err)
                {
                    Trace.TraceError($"Except card for {ns}/{localID}: {err}");
                    return StatusCode(500, err.ToString());
                }
            }
            catch (ArgumentException ae) {
                Trace.TraceError(ae.ToString());
                return BadRequest(ae);
            }
        }

        
        [HttpPut("{nsStr}/{localIDstr}/features/{featuresIdentStr}")]
        public async Task<IActionResult> PutFeatures(string nsStr, string localIDstr, string featuresIdentStr, [FromBody] JsonPoco.FeaturesPOCO features)
        {
            try
            {
                // verifying inputs
                AsciiIdentifier ns = new(nsStr);
                AsciiIdentifier localID = new(localIDstr);
                AsciiIdentifier featuresIdent = new(featuresIdentStr);
                try
                {
                    Trace.TraceInformation($"Setting features {featuresIdent} for {ns}/{localID}");
                    await this.storage.SetCardFeatureVectorAsync(ns, localID, featuresIdent, features.Features);
                    Trace.TraceInformation($"Successfully set features {featuresIdent} for {ns}/{localID}");
                    return Ok();
                }
                catch (Exception err)
                {
                    Trace.TraceError($"Except card for {ns}/{localID}: {err}");
                    return StatusCode(500, err.ToString());
                }
            }
            catch (ArgumentException ae) {
                Trace.TraceError(ae.ToString());
                return BadRequest(ae);
            }
        }

        // PUT <PetCardsController>
        [HttpPut("{nsStr}/{localIDstr}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Put(string nsStr, string localIDstr, [FromBody] PetCard value)
        {
            try
            {
                // verifying inputs
                AsciiIdentifier ns = new(nsStr);
                AsciiIdentifier localID = new(localIDstr);

                try
                {
                    Trace.TraceInformation($"Storing {ns}/{localID} into storage");
                    var res = await this.storage.SetPetCardAsync(ns, localID, value);
                    if (res)
                    {
                        Trace.TraceInformation($"Stored {ns}/{localID}");
                        return CreatedAtAction(nameof(Get),new { ns= ns, localID = localID },null);
                    }
                    else
                    {
                        Trace.TraceWarning($"Card {ns}/{localID} already exists");
                        return Conflict();
                    }
                }
                catch (Exception err)
                {
                    Trace.TraceError($"Exception while storing {ns}/{localID}: {err}");
                    return StatusCode(500, err.ToString());
                }
            }
            catch (ArgumentException ae)
            {
                Trace.TraceError(ae.ToString());
                return BadRequest(ae);
            }
        }

        // DELETE <PetCardsController>/pet911ru/rf123
        [HttpDelete("{nsStr}/{localIDstr}")]
        public async Task<ActionResult> Delete(string nsStr, string localIDstr)
        {
            try
            {
                // verifying inputs
                AsciiIdentifier ns = new(nsStr);
                AsciiIdentifier localID = new(localIDstr);

                try
                {
                    Trace.TraceInformation($"Received delete request for {ns}/{localID}");
                    var res = await this.storage.DeletePetCardAsync(ns, localID);
                    if (res)
                    {
                        Trace.TraceInformation($"Successfully deleted {ns}/{localID} from storage");
                        return Ok();
                    }
                    else
                    {
                        Trace.TraceError($"Failed to delete {ns}/{localID}");
                        return StatusCode(500);
                    }
                }
                catch (Exception err)
                {
                    Trace.TraceError($"Exception while deleting {ns}/{localID}: {err}");
                    return StatusCode(500, err.ToString());
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
