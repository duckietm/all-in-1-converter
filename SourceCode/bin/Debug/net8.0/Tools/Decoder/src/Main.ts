import 'reflect-metadata';
import { container } from 'tsyringe';
import { FileUtilities } from './common';
import { Configuration } from './common/config/Configuration';
import { IConverter } from './common/converters/IConverter';
import { ConverterUtilities } from './converters/ConverterUtilities';

(async () =>
{
    try
    {
        const configurationContent = await FileUtilities.readFileAsString('./configuration.json');
        const config = container.resolve(Configuration);

        await config.init(JSON.parse(configurationContent));

        const converters = [];  // Removed unused converters

        const convertSwf = (process.argv.indexOf('--convert-swf') >= 0);                

        const utilities = container.resolve(ConverterUtilities);

        await utilities.downloadSwfTypes();

        process.exit();
    }

    catch (e)
    {
        console.error(e);
    }
})();
