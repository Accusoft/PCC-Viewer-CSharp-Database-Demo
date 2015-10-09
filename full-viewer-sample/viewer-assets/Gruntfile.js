/**
 * Gruntfile.js
 *
 * Copyright (c) 2015 Accusoft Corporation. All rights reserved.
 */

/* jshint node: true */

module.exports = function(grunt) {
    grunt.initConfig({
        less: {
            viewer: {
                options: {
                    compress: false,
                    optimization: 2,
                    sourceMap: true,
                    sourceMapFilename: 'css/viewer.css.map',
                    sourceMapURL: 'viewer.css.map'
                },
                files: {
                    "css/viewer.css": "less/viewer.less"
                }
            },
            fonts: {
                options: {
                    compress: false,
                    optimization: 2,
                },
                files: {
                    "css/fonts.css": "less/fonts.less"
                }
            },
            legacy: {
                options: {
                    compress: false,
                    optimization: 2,
                },
                files: {
                    "css/legacy.css": "less/legacy.less"
                }
            },
            prod: {
                options: {
                    compress: false,
                    optimization: 2,
                },
                files: {
                    "css/viewer.css": "less/viewer.less",
                    "css/fonts.css": "less/fonts.less",
                    "css/legacy.css": "less/legacy.less"
                }
            }
        },
        watch: {
            styles: {
                files: ['less/**/*.less'],
                tasks: ['builddev'],
                options: {
                  nospawn: true
                }
            }
        }
    });

    // Load tasks from plugins in NPM
    grunt.loadNpmTasks('grunt-contrib-less');
    grunt.loadNpmTasks('grunt-contrib-watch');

    // During development, add the watch task to build when a change occurs
    grunt.registerTask('dev', ['builddev', 'watch']);
    
    grunt.registerTask('builddev', ['less:viewer', 'less:fonts', 'less:legacy']);
    grunt.registerTask('buildprod', ['less:prod']);
    
    // By default, only run the build tasks
    grunt.registerTask('default', ['buildprod']);
};