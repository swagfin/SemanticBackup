$(function () {

    'use strict'

    $('[data-toggle="tooltip"]').tooltip()

    if ($('.aside-backdrop').length === 0) {
        $('body').append('<div class="aside-backdrop"></div>');
    }

    var mql = window.matchMedia('(min-width:992px) and (max-width: 1199px)');

    function doMinimize(e) {
        if (e.matches) {
            $('.aside').addClass('minimize');
        } else {
            $('.aside').removeClass('minimize');
        }
    }

    mql.addListener(doMinimize);
    doMinimize(mql);

    $('.aside-menu-link').on('click', function (e) {
        e.preventDefault()

        if (window.matchMedia('(min-width: 992px)').matches) {
            $(this).closest('.aside').toggleClass('minimize');
        } else {

            $('body').toggleClass('show-aside');
        }
    })

    $('.nav-aside .with-sub').on('click', '.nav-link', function (e) {
        e.preventDefault();

        $(this).parent().siblings().removeClass('show');
        $(this).parent().toggleClass('show');
    })

    $('body').on('mouseenter', '.minimize .aside-body', function (e) {
        console.log('e');
        $(this).parent().addClass('maximize');
    })

    $('body').on('mouseleave', '.minimize .aside-body', function (e) {
        $(this).parent().removeClass('maximize');
    })

    $('body').on('click', '.aside-backdrop', function (e) {
        $('body').removeClass('show-aside');
    })
})
