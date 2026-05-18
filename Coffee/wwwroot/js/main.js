(function ($) {
    "use strict";
    
    function showCoffeeDropdown($dropdown) {
        clearTimeout($dropdown.data('coffeeHoverTimer'));
        $dropdown.addClass('show');
        $dropdown.find('.dropdown-toggle').attr('aria-expanded', 'true');
        $dropdown.find('.dropdown-menu').addClass('show');
    }

    function hideCoffeeDropdown($dropdown) {
        clearTimeout($dropdown.data('coffeeHoverTimer'));
        $dropdown.removeClass('show');
        $dropdown.find('.dropdown-toggle').attr('aria-expanded', 'false');
        $dropdown.find('.dropdown-menu').removeClass('show');
    }

    function scheduleCoffeeDropdownHide($dropdown) {
        clearTimeout($dropdown.data('coffeeHoverTimer'));

        var timer = window.setTimeout(function () {
            hideCoffeeDropdown($dropdown);
        }, 180);

        $dropdown.data('coffeeHoverTimer', timer);
    }

    // Desktop hover dropdown without the flicker caused by click toggling.
    $(document).ready(function () {
        function toggleNavbarMethod() {
            var $dropdowns = $('.navbar .dropdown');
            var $hoverTargets = $dropdowns.find('.dropdown-toggle, .dropdown-menu');

            $dropdowns.off('.coffeeHover');
            $hoverTargets.off('.coffeeHover');

            if ($(window).width() > 992) {
                $dropdowns.on('mouseenter.coffeeHover', function () {
                    showCoffeeDropdown($(this));
                }).on('mouseleave.coffeeHover', function () {
                    scheduleCoffeeDropdownHide($(this));
                }).on('focusin.coffeeHover', function () {
                    showCoffeeDropdown($(this));
                }).on('focusout.coffeeHover', function (event) {
                    if ($(this).has(event.relatedTarget).length === 0) {
                        scheduleCoffeeDropdownHide($(this));
                    }
                });

                $hoverTargets.on('mouseenter.coffeeHover', function () {
                    showCoffeeDropdown($(this).closest('.dropdown'));
                }).on('mouseleave.coffeeHover', function () {
                    scheduleCoffeeDropdownHide($(this).closest('.dropdown'));
                });
            } else {
                $dropdowns.removeClass('show');
                $dropdowns.find('.dropdown-menu').removeClass('show');
                $dropdowns.find('.dropdown-toggle').attr('aria-expanded', 'false');
            }
        }

        toggleNavbarMethod();
        $(window).resize(toggleNavbarMethod);
    });
    
    
    // Back to top button
    $(window).scroll(function () {
        if ($(this).scrollTop() > 100) {
            $('.back-to-top').fadeIn('slow');
        } else {
            $('.back-to-top').fadeOut('slow');
        }
    });
    $('.back-to-top').click(function () {
        $('html, body').animate({scrollTop: 0}, 1500, 'easeInOutExpo');
        return false;
    });
    

    // Date and time picker
    $('.date').datetimepicker({
        format: 'L'
    });
    $('.time').datetimepicker({
        format: 'LT'
    });


    // Testimonials carousel
    $(".testimonial-carousel").owlCarousel({
        autoplay: true,
        smartSpeed: 1500,
        margin: 30,
        dots: true,
        loop: true,
        center: true,
        responsive: {
            0:{
                items:1
            },
            576:{
                items:1
            },
            768:{
                items:2
            },
            992:{
                items:3
            }
        }
    });

    function syncCoffeeHeader() {
        $('.coffee-topbar').toggleClass('coffee-topbar-scrolled', $(window).scrollTop() > 24);
    }

    syncCoffeeHeader();
    $(window).on('scroll', syncCoffeeHeader);
    
})(jQuery);

