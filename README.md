# README #

This an application to Configure IDTech Devices.

### What is this repository for? ###

* Quick summary
* Version

### How do I get set up? ###

* Summary of set up
* Configuration
* Dependencies
* Database configuration
* How to run tests
* Deployment instructions

### Contribution guidelines ###

* Writing tests
* Code review
* Other guidelines

### HISTORY ###

* 20190320 - Initial repository.
* 20190405 - Cleaned up tabs prior to merge.
* 20190408 - Implemented FIRMWARE update.
           - First pass at merging projects.
* 20190409 - Implemented Augusta Configuration reader.
* 20190410 - Corrected packages warning.
* 20190415 - Implemented serial number tag 9F1E in compressed form.
           - Update AppSettings after logging option change.
* 20190417 - Added initial unit tests.
* 20190425 - Added Advanced tab logging option.
* 20190426 - Cleaned up projects dependencies.
* 20190429 - Added check for Major configuration.
* 20190430 - Separated General Settings from MSR Settings.
           - Added extended firmware version.
* 20190501 - VP5300 Fixes to process configuration file.
* 20190502 - Fixes for EMV transaction processing.
* 20190503 - Reorganized SETTINGS tab page.
* 20190506 - Improved card processing workflow.
           - Improved keyboard mode workflow.
* 20190507 - Streamlined device specific methods.
* 20190508 - Fixes to JSON file processing.
           - Changed offending TAG "9F40" value "6000F05001" to "F000F0A001".
* 20190509 - Implemented error states for error on Set Terminal Data.
* 20190510 - Implemented Beep and LED controls configuration.
           - Enhanced user error reporting in Settings.
* 20190515 - Updated SphereConfiguration.json with Contactless ApplicationFlow.
* 20190603 - Renamed configuration file and coded tag 9F4E to process value from ConfigurationID.Version.
* 20190606 - Added documentation for TAG 9F1E Encoding/Decoding algorithm.
* 20190619 - Fixes for Augusta SRED switching to keyboard mode.
           - Fixes to processing of AIDS not found in device AIDS list.
           - Fixes to processing of CAPKS not found in device CAPKS list.
* 20190626 - Implemented ContactOverrideTags.
* 20190628 - Added MSR, ICC, and General Group default reset.
* 20190705 - Added CombinedSerialKernelTag support for TAG DFED22.
* 20190717 - Cleaned up device methods.
* 20190812 - Formatting clean up.
* 20210107 - SRedKey2 initial implementation.
* 20210108 - SRedKey initial implementation.
